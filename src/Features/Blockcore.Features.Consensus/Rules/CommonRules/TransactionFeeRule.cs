using Blockcore.Consensus.Rules;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.CommonRules
{
    /// <summary>Verify the fees in for transactions added to a block.</summary>
    public class TransactionFeeRule : IntegrityValidationConsensusRule
    {
        /// <summary>The consensus of the parent Network.</summary>
        protected IConsensus consensus;

        /// <summary>A base skeleton method that is implemented by networks.</summary>
        public override void Run(RuleContext context) { }

        public override void Initialize()
        {
            base.Initialize();

            this.consensus = this.Parent.Network.Consensus;
        }

        /// True returned if fee for the transaction is too low, otherwise false is returned.</exception>
        public virtual bool IsFeeTooLow(Money fee, Money packageFees, Transaction _)
        {
            bool result = false;
            if (packageFees < fee)
            {
                result = true;
            }
            return result;
        }

        /// Get the transaction fee.
        public virtual Money GetTransactionFee(long minTxFee, int transactionSize, string _)
        {
            return new FeeRate(minTxFee).GetFee(transactionSize);
        }

        /// Get minimum transaction fee.
        public virtual Money GetMinimumTransactionFee(long minTxFee, string _)
        {
            return new Money(minTxFee, MoneyUnit.Satoshi);
        }
    }
}