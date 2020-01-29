using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Consensus.Rules;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    /// Push the modified <see cref="UnspentOutputSet"/> back to the underline cache.
    /// </summary>
    public class PushCoinviewRule : UtxoStoreConsensusRule
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
    public class FlushCoinviewRule : UtxoStoreConsensusRule
    {
        private readonly IInitialBlockDownloadState initialBlockDownloadState;

        public FlushCoinviewRule(IInitialBlockDownloadState initialBlockDownloadState)
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
    public class FetchCoinviewRule : UtxoStoreConsensusRule
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

            OutPoint[] ids = this.coinviewHelper.GetIdsToFetch(context.ValidationContext.BlockToValidate, context.Flags.EnforceBIP30);
            FetchCoinsResponse coins = this.PowParent.UtxoSet.FetchCoins(ids);
            utxoRuleContext.UnspentOutputSet.SetCoins(coins.UnspentOutputs.Values.ToArray());

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Legacy class for coins that did not upgrade.
    /// </summary>
    public class SaveCoinviewRule : PushCoinviewRule
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
    public class LoadCoinviewRule : FetchCoinviewRule
    {
    }
}