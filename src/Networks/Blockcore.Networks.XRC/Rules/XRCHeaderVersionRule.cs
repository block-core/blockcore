using Blockcore.Consensus.Rules;
using Blockcore.Features.Consensus.Rules.CommonRules;

namespace Blockcore.Networks.XRC.Rules
{
    /// <summary>
    /// Checks if <see cref="XRCMain"/> network block's header has a valid block version.
    /// </summary>
    public class XRCHeaderVersionRule : HeaderVersionRule
    {
        /// <inheritdoc />
        public override void Run(RuleContext context)
        {
        }
    }
}