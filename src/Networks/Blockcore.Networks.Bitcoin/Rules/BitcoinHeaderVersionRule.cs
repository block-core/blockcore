using Blockcore.Consensus.Rules;
using Blockcore.Features.Consensus.Rules.CommonRules;

namespace Blockcore.Networks.Bitcoin.Rules
{
    /// <summary>
    /// Checks if <see cref="BitcoinMain"/> network block's header has a valid block version.
    /// <seealso cref="BitcoinActivationRule" />
    /// </summary>
    public class BitcoinHeaderVersionRule : HeaderVersionRule
    {
        /// <inheritdoc />
        public override void Run(RuleContext context)
        {
            // This is a stub rule - all version numbers are valid except those rejected by BitcoinActivationRule based
            // on the combination of their block height and version number.

            // All networks need a HeaderVersionRule implementation, as ComputeBlockVersion is used for block creation.
        }
    }
}