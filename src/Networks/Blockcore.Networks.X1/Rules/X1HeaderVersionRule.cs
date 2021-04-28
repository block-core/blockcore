using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.X1.Rules
{
    /// <summary>
    /// Checks if <see cref="X1Main"/> network block's header has a valid block version.
    /// </summary>
    public class X1HeaderVersionRule : HeaderVersionRule
    {
        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.BadVersion">Thrown if block's version is outdated or otherwise invalid.</exception>
        public override void Run(RuleContext context)
        {
            Guard.NotNull(context.ValidationContext.ChainedHeaderToValidate, nameof(context.ValidationContext.ChainedHeaderToValidate));

            ChainedHeader chainedHeader = context.ValidationContext.ChainedHeaderToValidate;

            // ODX will always use BIP9 enabled blocks.
            if ((chainedHeader.Header.Version & ThresholdConditionCache.VersionbitsTopMask) != ThresholdConditionCache.VersionbitsTopBits)
            {
                this.Logger.LogTrace("(-)[BAD_VERSION]");
                ConsensusErrors.BadVersion.Throw();
            }
        }
    }
}