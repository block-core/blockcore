using System;
using System.Threading.Tasks;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.X1.Rules
{
    /// <summary>
    /// Checks if transactions match the white-listing criteria. This rule and <see cref="X1OutputNotWhitelistedMempoolRule"/> must correspond.
    /// </summary>
    public class X1OutputNotWhitelistedRule : PartialValidationConsensusRule
    {
        public override Task RunAsync(RuleContext context)
        {
            var block = context.ValidationContext.BlockToValidate;
            var isPosBlock = block.Transactions.Count >= 2 && block.Transactions[1].IsCoinStake;

            foreach (var transaction in context.ValidationContext.BlockToValidate.Transactions)
            {
                if (transaction.IsCoinStake)
                    continue;

                if (transaction.IsCoinBase && isPosBlock)
                    continue;

                foreach (var output in transaction.Outputs)
                {
                    if (IsOutputWhitelisted(output))
                        continue;

                    this.Logger.LogTrace($"(-)[FAIL_{nameof(X1OutputNotWhitelistedRule)}]".ToUpperInvariant());
                    X1ConsensusErrors.OutputNotWhitelisted.Throw();
                }
            }

            return Task.CompletedTask;
        }

        public static bool IsOutputWhitelisted(TxOut txOut)
        {
            if (txOut == null || txOut.ScriptPubKey == null || txOut.ScriptPubKey.Length == 0)
                throw new ArgumentException("This method expects a TxOut with a non-empty ScriptPubKey.");

            byte[] raw = txOut.ScriptPubKey.ToBytes();

            const int witnessVersion = 0;

            // P2WPKH
            if (raw.Length == 22 && raw[0] == witnessVersion && raw[1] == 20)
                return true;

            // P2WSH
            if (raw.Length == 34 && raw[0] == witnessVersion && raw[1] == 32)
                return true;

            // OP_RETURN
            if (raw[0] == (byte)OpcodeType.OP_RETURN)
                return true;

            // WITNESS_V1_TAPROOT
            if (raw.Length == 34 && raw[0] == 1)
                return true;

            return false;
        }
    }
}