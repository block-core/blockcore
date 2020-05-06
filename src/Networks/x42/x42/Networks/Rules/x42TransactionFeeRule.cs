using System;
using System.Linq;
using Blockcore.Features.Consensus.Rules.CommonRules;
using NBitcoin;

namespace x42.Networks.Consensus
{
    public class x42TransactionFeeRule : TransactionFeeRule
    {
        /// <inheritdoc />
        /// For x42, when there are zero fee's we want to still include the transaction.
        public override bool IsFeeTooLow(Money fee, Money packageFees, Transaction transaction)
        {
            bool result = false;
            int opReturnCount = transaction.Outputs.Select(o => o.ScriptPubKey.ToBytes(true)).Count(b => IsOpReturn(b));

            // If there is OP_RETURN data, we will want to make sure there is a fee.
            if (opReturnCount > 0)
            {
                if (packageFees < fee)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override Money GetMinimumTransactionFee(long minTxFee, string OpReturnData)
        {
            var consensus = (x42Consensus)this.consensus;
            var minTrxFee = new Money(minTxFee, MoneyUnit.Satoshi);

            // Additional fee for OpReturnData
            if (!string.IsNullOrEmpty(OpReturnData))
            {
                minTrxFee = Math.Max(minTrxFee, consensus.MinOPReturnFee);
            }

            return minTrxFee;
        }

        private bool IsOpReturn(byte[] bytes)
        {
            return bytes.Length > 0 && bytes[0] == (byte)OpcodeType.OP_RETURN;
        }
    }
}