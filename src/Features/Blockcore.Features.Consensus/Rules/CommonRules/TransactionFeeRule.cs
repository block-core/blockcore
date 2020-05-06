using System.Threading.Tasks;
using Blockcore.Consensus.Rules;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.CommonRules
{
    /// <summary>Verify the fees in for transactions added to a block.</summary>
    public class TransactionFeeRule : IntegrityValidationConsensusRule
    {
        /// <summary>A base skeleton method that is implemented by networks.</summary>
        public override void Run(RuleContext context) { }

        /// True returned if fee is sufficient to add to a block, or otherwise false is returned.</exception>
        public virtual bool IsFeeTooLow(Money fee, Money packageFees, Transaction _)
        {
            bool result = false;
            if (packageFees < fee)
            {
                result = true;
            }
            return result;
        }
    }
}