using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.CommonRules
{
    /// <summary>Calculate the difficulty for a POW network and check that it is correct.</summary>
    public class CheckDifficultyPowRule : HeaderValidationConsensusRule
    {
        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.HighHash"> Thrown if block doesn't have a valid PoS header.</exception>
        public override void Run(RuleContext context)
        {
            if (!context.ValidationContext.ChainedHeaderToValidate.Header.CheckProofOfWork())
                ConsensusErrors.HighHash.Throw();

            Target nextWorkRequired = context.ValidationContext.ChainedHeaderToValidate.GetWorkRequired(this.Parent.Network.Consensus);

            BlockHeader header = context.ValidationContext.ChainedHeaderToValidate.Header;

            // Check proof of work.
            if (header.Bits != nextWorkRequired)
            {
                this.Logger.LogTrace("(-)[BAD_DIFF_BITS]");
                ConsensusErrors.BadDiffBits.Throw();
            }
        }
    }
}