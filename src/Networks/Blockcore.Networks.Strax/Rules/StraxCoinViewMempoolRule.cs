using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.MemoryPool.Rules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.Strax.Rules
{
    /// <summary>
    /// Validates the transaction with the coin view.
    /// Checks if already in coin view, and missing and unavailable inputs.
    /// </summary>
    public class StraxCoinViewMempoolRule : CheckCoinViewMempoolRule
    {
        public StraxCoinViewMempoolRule(Network network,
            ITxMempool mempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            ILoggerFactory loggerFactory) : base(network, mempool, mempoolSettings, chainIndexer, loggerFactory)
        {
        }

        /// <remarks>Also see <see cref="StraxCoinviewRule"/></remarks>>
        public override void CheckTransaction(MempoolValidationContext context)
        {
            base.CheckTransaction(context);

            foreach (TxIn txin in context.Transaction.Inputs)
            {
                // We expect that by this point the base rule will have checked for missing inputs.
                UnspentOutput unspentOutput = context.View.Set.AccessCoins(txin.PrevOut);
                if (unspentOutput?.Coins == null)
                {
                    context.State.MissingInputs = true;
                    this.logger.LogTrace("(-)[FAIL_MISSING_INPUTS_ACCESS_COINS]");
                    context.State.Fail(MempoolErrors.MissingOrSpentInputs).Throw();
                }

                if (unspentOutput.Coins.TxOut.ScriptPubKey == StraxCoinstakeRule.CirrusRewardScript)
                {
                    this.logger.LogDebug($"Reward distribution transaction seen in mempool, paying to '{unspentOutput.Coins.TxOut.ScriptPubKey}'.");

                    foreach (TxOut output in context.Transaction.Outputs)
                    {
                        if (output.ScriptPubKey.IsUnspendable)
                        {
                            if (output.Value != 0)
                            {
                                this.logger.LogTrace("(-)[INVALID_REWARD_OP_RETURN_SPEND]");
                                context.State.Fail(new MempoolError(MempoolErrors.RejectInvalid, "bad-cirrus-reward-tx-opreturn-not-zero"), "Cirrus reward transaction invalid, op_return value is not 0.").Throw();
                            }

                            continue;
                        }

                        // Every other (spendable) output must go to the multisig
                        if (output.ScriptPubKey != ((StraxBaseNetwork)this.network).Federations.GetOnlyFederation().MultisigScript.PaymentScript)
                        {
                            this.logger.LogTrace("(-)[INVALID_REWARD_SPEND_DESTINATION]");
                            context.State.Fail(new MempoolError(MempoolErrors.RejectInvalid, "bad-cirrus-reward-tx-reward-dest-invalid"), "Cirrus reward transaction invalid, reward destination invalid.").Throw();
                        }
                    }
                }
            }
        }
    }
}
