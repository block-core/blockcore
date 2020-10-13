using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Interfaces;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.UtxosetRules
{
    /// <summary>
    /// Push the modified <see cref="UnspentOutputSet"/> back to the underline cache.
    /// </summary>
    public class PushUtxosetRule : UtxoStoreConsensusRule
    {
        /// <inheritdoc />
        public override Task RunAsync(RuleContext context)
        {
            ChainedHeader oldBlock = context.ValidationContext.ChainedHeaderToValidate.Previous;
            ChainedHeader nextBlock = context.ValidationContext.ChainedHeaderToValidate;

            // Persist the changes to the coinview. This will likely only be stored in memory,
            // unless the coinview treashold is reached.
            this.Logger.LogDebug("Saving coinview changes.");
            var utxoRuleContext = context as UtxoRuleContext;
            this.PowParent.UtxoSet.SaveChanges(utxoRuleContext.UnspentOutputSet.GetCoins(), new HashHeightPair(oldBlock), new HashHeightPair(nextBlock));

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Track flush operations in a separate rule to better minitor its performance.
    /// </summary>
    public class FlushUtxosetRule : UtxoStoreConsensusRule
    {
        private readonly IInitialBlockDownloadState initialBlockDownloadState;
        private readonly IChainRepository chainRepository;
        private readonly ChainIndexer chainIndexer;
        private readonly INodeLifetime nodeLifetime;
        private readonly IChainState chainState;

        public FlushUtxosetRule(
            IInitialBlockDownloadState initialBlockDownloadState,
            IChainRepository chainRepository,
            ChainIndexer chainIndexer,
            INodeLifetime nodeLifetime,
            IChainState chainState)
        {
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.chainRepository = chainRepository;
            this.chainIndexer = chainIndexer;
            this.nodeLifetime = nodeLifetime;
            this.chainState = chainState;
        }

        /// <inheritdoc />
        public override Task RunAsync(RuleContext context)
        {
            if (this.PowParent.UtxoSet is CachedCoinView cachedCoinView)
            {
                bool inIBD = this.initialBlockDownloadState.IsInitialBlockDownload();

                if (!inIBD || cachedCoinView.ShouldFlush())
                {
                    // wait for blockstore to catch up
                    this.WaitForBlockstore(this.PowParent.UtxoSet as CachedCoinView);

                    // flush chain repository
                    this.FlushChainRepo();

                    cachedCoinView.Flush(true);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Before we continue to persist coindb we need to make sure the
        /// store is not behind our tip, if it is we will wait to let store
        /// catchup even if it means we need to block consensus form advancing
        /// </summary>
        /// <param name="cachedCoinView"></param>
        private void WaitForBlockstore(CachedCoinView cachedCoinView)
        {
            if (this.chainState.BlockStoreTip != null)
            {
                int delaySec = 3;
                int rewindDataWindow = cachedCoinView.CalculateRewindWindow();
                HashHeightPair cachedCoinViewTip = cachedCoinView.GetTipHash();

                while (cachedCoinViewTip.Height - rewindDataWindow + 1 > this.chainState.BlockStoreTip.Height)
                {
                    if (this.nodeLifetime.ApplicationStopping.IsCancellationRequested)
                    {
                        // node is closing do nothing.
                        return;
                    }

                    // wait 3 seconds to let blockstore catch up
                    this.Logger.LogWarning("Store tip `{0}` is behind coindb rewind data tip `{1}` waiting {2} seconds to let store catch up", this.chainState.BlockStoreTip.Height, cachedCoinViewTip.Height - rewindDataWindow + 1, delaySec);
                    Task.Delay(delaySec * 1000).Wait();
                }
            }
        }

        /// <summary>
        /// Flush the chain repository before flushing the consensus coindb.
        /// This is in order to avoid consensus being ahead of the chain of
        /// headers in case of a node crash.
        /// </summary>
        private void FlushChainRepo()
        {
            this.chainRepository.SaveAsync(this.chainIndexer).Wait();
        }
    }

    /// <summary>
    /// Load a blocks utxos to <see cref="UnspentOutputSet"/> a workable data set.
    /// </summary>
    public class FetchUtxosetRule : UtxoStoreConsensusRule
    {
        /// <inheritdoc />
        public override Task RunAsync(RuleContext context)
        {
            // Check that the current block has not been reorged.
            // Catching a reorg at this point will not require a rewind.
            if (context.ValidationContext.BlockToValidate.Header.HashPrevBlock != this.Parent.ChainState.ConsensusTip.HashBlock)
            {
                this.Logger.LogDebug("Reorganization detected.");
                ConsensusErrors.InvalidPrevTip.Throw();
            }

            var utxoRuleContext = context as UtxoRuleContext;

            // Load the UTXO set of the current block. UTXO may be loaded from cache or from disk.
            // The UTXO set is stored in the context.
            this.Logger.LogDebug("Loading UTXO set of the new block.");
            utxoRuleContext.UnspentOutputSet = new UnspentOutputSet();

            bool enforceBIP30 = context.ValidationContext.ChainedHeaderToValidate.Height > this.Parent.Checkpoints.LastCheckpointHeight ? context.Flags.EnforceBIP30 : false;
            OutPoint[] ids = this.coinviewHelper.GetIdsToFetch(context.ValidationContext.BlockToValidate, enforceBIP30);
            FetchCoinsResponse coins = this.PowParent.UtxoSet.FetchCoins(ids);
            utxoRuleContext.UnspentOutputSet.SetCoins(coins.UnspentOutputs.Values.ToArray());

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Legacy class for coins that did not upgrade.
    /// </summary>
    [Obsolete("Use PushUtxosetRule instead")]
    public class SaveCoinviewRule : PushUtxosetRule
    {
        /// <inheritdoc />
        public override Task RunAsync(RuleContext context)
        {
            base.RunAsync(context);

            // Use the default flush condition to decide if flush is required (currently set to every 60 seconds)
            if (this.PowParent.UtxoSet is CachedCoinView cachedCoinView)
                cachedCoinView.Flush(false);

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Legacy class for coins that did not upgrade.
    /// </summary>
    [Obsolete("Use FetchUtxosetRule instead")]
    public class LoadCoinviewRule : FetchUtxosetRule
    {
    }
}