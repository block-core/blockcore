using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.MemoryPool.Rules
{
    /// <summary>
    /// Validates the transaction with the coin view.
    /// Checks if already in coin view, and missing and unavailable inputs.
    /// </summary>
    public class CheckCoinViewMempoolRule : MempoolRule
    {
        public CheckCoinViewMempoolRule(Network network,
            ITxMempool mempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            ILoggerFactory loggerFactory) : base(network, mempool, mempoolSettings, chainIndexer, loggerFactory)
        {
        }

        public override void CheckTransaction(MempoolValidationContext context)
        {
            Guard.Assert(context.View != null);

            context.LockPoints = new LockPoints();

            // Do we already have it?
            if (context.View.HaveTransaction(context.TransactionHash))
            {
                this.logger.LogTrace("(-)[INVALID_ALREADY_KNOWN]");
                context.State.Invalid(MempoolErrors.AlreadyKnown).Throw();
            }

            // Do all inputs exist?
            foreach (TxIn txin in context.Transaction.Inputs)
            {
                if (!context.View.HaveCoins(txin.PrevOut))
                {
                    // Assume this might be an orphan tx for which we just haven't seen parents yet
                    context.State.MissingInputs = true;
                    this.logger.LogTrace("(-)[FAIL_MISSING_INPUTS]");
                    context.State.Fail(MempoolErrors.MissingOrSpentInputs).Throw();
                }
            }
        }
    }
}