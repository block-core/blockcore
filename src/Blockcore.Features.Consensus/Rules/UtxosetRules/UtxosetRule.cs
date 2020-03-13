using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.Rules;
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

        public FlushUtxosetRule(IInitialBlockDownloadState initialBlockDownloadState)
        {
            this.initialBlockDownloadState = initialBlockDownloadState;
        }

        /// <inheritdoc />
        public override Task RunAsync(RuleContext context)
        {
            if (this.PowParent.UtxoSet is CachedCoinView cachedCoinView)
            {
                bool inIBD = this.initialBlockDownloadState.IsInitialBlockDownload();
                cachedCoinView.Flush(force: !inIBD);
            }

            return Task.CompletedTask;
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