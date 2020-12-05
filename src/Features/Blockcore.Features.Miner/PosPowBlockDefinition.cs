using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.Interfaces;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Mining;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Miner
{
    /// <summary>
    /// Defines how a proof of work block will be built on a proof of stake network.
    /// </summary>
    public sealed class PosPowBlockDefinition : BlockDefinition
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Database of stake related data for the current blockchain.</summary>
        private readonly IStakeChain stakeChain;

        /// <summary>Provides functionality for checking validity of PoS blocks.</summary>
        private readonly IStakeValidator stakeValidator;

        /// <summary>
        /// The POS rule to determine the allowed drift in time between nodes.
        /// </summary>
        private PosFutureDriftRule futureDriftRule;

        public PosPowBlockDefinition(
            IConsensusManager consensusManager,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory,
            ITxMempool mempool,
            MempoolSchedulerLock mempoolLock,
            Network network,
            MinerSettings minerSettings,
            IStakeChain stakeChain,
            IStakeValidator stakeValidator,
            NodeDeployments nodeDeployments)
            : base(consensusManager, dateTimeProvider, loggerFactory, mempool, mempoolLock, minerSettings, network, nodeDeployments)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.stakeChain = stakeChain;
            this.stakeValidator = stakeValidator;
        }

        /// <inheritdoc/>
        public override void AddToBlock(TxMempoolEntry mempoolEntry)
        {
            this.AddTransactionToBlock(mempoolEntry.Transaction);
            this.UpdateBlockStatistics(mempoolEntry);
            this.UpdateTotalFees(mempoolEntry.Fee);
        }

        /// <inheritdoc/>
        public override BlockTemplate Build(ChainedHeader chainTip, Script scriptPubKey)
        {
            this.OnBuild(chainTip, scriptPubKey);

            return this.BlockTemplate;
        }

        /// <inheritdoc/>
        public override void UpdateHeaders()
        {
            base.UpdateBaseHeaders();

            this.block.Header.Bits = this.stakeValidator.GetNextTargetRequired(this.stakeChain, this.ChainTip, this.Network.Consensus, false);
        }

        /// <inheritdoc/>
        protected override bool TestPackage(TxMempoolEntry entry, long packageSize, long packageSigOpsCost)
        {
            if (this.futureDriftRule == null)
                this.futureDriftRule = this.ConsensusManager.ConsensusRules.GetRule<PosFutureDriftRule>();

            long adjustedTime = this.DateTimeProvider.GetAdjustedTimeAsUnixTimestamp();
            long latestValidTime = adjustedTime + this.futureDriftRule.GetFutureDrift(adjustedTime);

            // We can include txes with timestamp greater than header's timestamp and those txes are invalid to have in block.
            // However this is needed in order to avoid recreation of block template on every attempt to find kernel.
            // When kernel is found txes with timestamp greater than header's timestamp are removed.
            if (entry.Transaction is IPosTransactionWithTime posTrx)
            {
                if (posTrx.Time > latestValidTime)
                {
                    this.logger.LogDebug("Transaction '{0}' has timestamp of {1} but latest valid tx time that can be mined is {2}.", entry.TransactionHash, posTrx.Time, latestValidTime);
                    this.logger.LogTrace("(-)[TOO_EARLY_TO_MINE_TX]:false");
                    return false;
                }
            }

            return base.TestPackage(entry, packageSize, packageSigOpsCost);
        }

    }
}