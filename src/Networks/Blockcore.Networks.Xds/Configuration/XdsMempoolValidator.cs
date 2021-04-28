using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Configuration;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.Xds.Configuration
{
    public class XdsMempoolValidator : IMempoolValidator
    {
        private readonly Network network;
        private readonly ITxMempool txMemPool;
        private readonly MempoolSchedulerLock mempoolLock;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly MempoolSettings mempoolSettings;
        private readonly ChainIndexer chainIndexer;
        private readonly ICoinView coinView;
        private readonly IEnumerable<IMempoolRule> mempoolRules;
        private readonly ILogger logger;
        private readonly FeeRate minRelayTxFee;

        public XdsMempoolValidator(
            Network network,
        ITxMempool txMemPool,
        MempoolSchedulerLock mempoolLock,
            IDateTimeProvider dateTimeProvider,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            ICoinView coinView,
            ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            IEnumerable<IMempoolRule> mempoolRules)
        {
            this.network = network;
            this.txMemPool = txMemPool;
            this.mempoolLock = mempoolLock;
            this.dateTimeProvider = dateTimeProvider;
            this.mempoolSettings = mempoolSettings;
            this.chainIndexer = chainIndexer;
            this.network = chainIndexer.Network;
            this.coinView = coinView;
            this.logger = loggerFactory.CreateLogger(GetType().FullName);
            this.PerformanceCounter = new MempoolPerformanceCounter(this.dateTimeProvider);
            this.minRelayTxFee = nodeSettings.MinRelayTxFeeRate;
            this.mempoolRules = mempoolRules.ToList();
        }

        #region IMempoolValidator

        /// <inheritdoc />
        public ConsensusOptions ConsensusOptions => this.network.Consensus.Options;

        /// <inheritdoc />
        public MempoolPerformanceCounter PerformanceCounter { get; }

        /// <inheritdoc />
        public Task<bool> AcceptToMemoryPool(MempoolValidationState state, Transaction tx)
        {
            state.AcceptTime = this.dateTimeProvider.GetTime();
            return AcceptToMemoryPoolWithTime(state, tx);
        }

        /// <inheritdoc />
        public async Task<bool> AcceptToMemoryPoolWithTime(MempoolValidationState state, Transaction tx)
        {
            try
            {
                await AcceptToMemoryPoolWorkerAsync(state, tx);
                //if (!res) {
                //    BOOST_FOREACH(const uint256& hashTx, vHashTxToUncache)
                //        pcoinsTip->Uncache(hashTx);
                //}

                if (state.IsInvalid)
                {
                    this.logger.LogTrace("(-):false");
                    return false;
                }

                this.logger.LogTrace("(-):true");
                return true;
            }
            catch (MempoolErrorException mempoolError)
            {
                this.logger.LogDebug("{0}:'{1}' ErrorCode:'{2}',ErrorMessage:'{3}'", nameof(MempoolErrorException), mempoolError.Message, mempoolError.ValidationState?.Error?.Code, mempoolError.ValidationState?.ErrorMessage);
                this.logger.LogTrace("(-)[MEMPOOL_EXCEPTION]:false");
                return false;
            }
            catch (ConsensusErrorException consensusError)
            {
                this.logger.LogDebug("{0}:'{1}' ErrorCode:'{2}',ErrorMessage:'{3}'", nameof(ConsensusErrorException), consensusError.Message, consensusError.ConsensusError?.Code, consensusError.ConsensusError?.Message);
                state.Error = new MempoolError(consensusError.ConsensusError);
                this.logger.LogTrace("(-)[CONSENSUS_EXCEPTION]:false");
                return false;
            }
        }

        /// <inheritdoc />
        public Task SanityCheck()
        {
            return Task.CompletedTask; // let's not pretend this is doing something!
        }

        #endregion IMempoolValidator

        /// <summary>
        /// Validates and then adds a transaction to memory pool.
        /// </summary>
        /// <param name="state">Validation state for creating the validation context.</param>
        /// <param name="tx">The transaction to validate.</param>
        private async Task AcceptToMemoryPoolWorkerAsync(MempoolValidationState state, Transaction tx)
        {
            var context = new MempoolValidationContext(tx, state);

            context.MinRelayTxFee = this.minRelayTxFee;

            // TODO: Convert these into rules too
            // this.PreMempoolChecks(context); - done!

            // Create the MemPoolCoinView and load relevant utxoset
            context.View = new MempoolCoinView(this.network, this.coinView, this.txMemPool, this.mempoolLock, this);

            // adding to the mem pool can only be done sequentially
            // use the sequential scheduler for that.
            await this.mempoolLock.WriteAsync(() =>
            {
                context.View.LoadViewLocked(context.Transaction);

                // If the transaction already exists in the mempool,
                // we only record the state but do not throw an exception.
                // This is because the caller will check if the state is invalid
                // and if so return false, meaning that the transaction should not be relayed.
                if (this.txMemPool.Exists(context.TransactionHash))
                {
                    state.Invalid(MempoolErrors.InPool);
                    this.logger.LogTrace("(-)[INVALID_TX_ALREADY_EXISTS]");
                    return;
                }

                foreach (IMempoolRule rule in this.mempoolRules)
                {
                    rule.CheckTransaction(context);
                }

                // Remove conflicting transactions from the mempool
                foreach (TxMempoolEntry it in context.AllConflicting)
                    this.logger.LogInformation($"Replacing tx {it.TransactionHash} with {context.TransactionHash} for {context.ModifiedFees - context.ConflictingFees} BTC additional fees, {context.EntrySize - context.ConflictingSize} delta bytes");

                this.txMemPool.RemoveStaged(context.AllConflicting, false);

                // This transaction should only count for fee estimation if
                // the node is not behind and it is not dependent on any other
                // transactions in the mempool
                bool validForFeeEstimation = IsCurrentForFeeEstimation() && this.txMemPool.HasNoInputsOf(tx);

                // Store transaction in memory
                this.txMemPool.AddUnchecked(context.TransactionHash, context.Entry, context.SetAncestors, validForFeeEstimation);

                // trim mempool and check if tx was trimmed
                if (!state.OverrideMempoolLimit)
                {
                    LimitMempoolSize(this.mempoolSettings.MaxMempool * 1000000, this.mempoolSettings.MempoolExpiry * 60 * 60);

                    if (!this.txMemPool.Exists(context.TransactionHash))
                    {
                        this.logger.LogTrace("(-)[FAIL_MEMPOOL_FULL]");
                        state.Fail(MempoolErrors.Full).Throw();
                    }
                }

                // do this here inside the exclusive scheduler for better accuracy
                // and to avoid springing more concurrent tasks later
                state.MempoolSize = this.txMemPool.Size;
                state.MempoolDynamicSize = this.txMemPool.DynamicMemoryUsage();

                this.PerformanceCounter.SetMempoolSize(state.MempoolSize);
                this.PerformanceCounter.SetMempoolDynamicSize(state.MempoolDynamicSize);
                this.PerformanceCounter.AddHitCount(1);
            });
        }

        /// <summary>
        /// Whether chain is currently valid for fee estimation.
        /// It should only count for fee estimation if the node is not behind.
        /// </summary>
        /// <returns>Whether current for fee estimation.</returns>
        private bool IsCurrentForFeeEstimation()
        {
            // TODO: implement method (find a way to know if in IBD)

            //if (IsInitialBlockDownload())
            //  return false;

            if (this.chainIndexer.Tip.Header.BlockTime.ToUnixTimeMilliseconds() < (this.dateTimeProvider.GetTime() - MempoolValidator.MaxFeeEstimationTipAge))
            {
                return false;
            }

            //if (chainActive.Height() < pindexBestHeader->nHeight - 1)
            //  return false;

            return true;
        }

        /// <summary>
        /// Trims memory pool to a new size.
        /// First expires transactions older than age.
        /// Then trims memory pool to limit if necessary.
        /// </summary>
        /// <param name="limit">New size.</param>
        /// <param name="age">AAge to use for calculating expired transactions.</param>
        private void LimitMempoolSize(long limit, long age)
        {
            int expired = this.txMemPool.Expire(this.dateTimeProvider.GetTime() - age);
            if (expired != 0)
                this.logger.LogInformation($"Expired {expired} transactions from the memory pool");

            var vNoSpendsRemaining = new List<uint256>();
            this.txMemPool.TrimToSize(limit, vNoSpendsRemaining);
        }
    }
}