using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.CommonRules
{
    /// <summary>
    /// A rule that will verify the block time drift is according to the PoS consensus rules.
    /// </summary>
    public class PosFutureDriftRule : HeaderValidationConsensusRule
    {
        /// <summary>The future drift in seconds.</summary>
        public const int FutureDriftSeconds = 15;

        /// <summary>Allow access to the POS parent.</summary>
        protected PosConsensusRuleEngine PosParent;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.PosParent = this.Parent as PosConsensusRuleEngine;

            Guard.NotNull(this.PosParent, nameof(this.PosParent));
        }

        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.BlockTimestampTooFar">The block timestamp is too far into the future.</exception>
        public override void Run(RuleContext context)
        {
            BlockHeader header = context.ValidationContext.ChainedHeaderToValidate.Header;

            long adjustedTime = this.Parent.DateTimeProvider.GetAdjustedTimeAsUnixTimestamp();

            // Check timestamp.
            if (header.Time > adjustedTime + this.GetFutureDrift(adjustedTime))
            {
                // The block can be valid only after its time minus the future drift.
                context.ValidationContext.RejectUntil = Utils.UnixTimeToDateTime(header.Time - this.GetFutureDrift(header.Time)).UtcDateTime;
                this.Logger.LogTrace("(-)[TIME_TOO_FAR]");
                ConsensusErrors.BlockTimestampTooFar.Throw();
            }
        }

        /// <summary>
        /// Gets future drift for the provided timestamp.
        /// </summary>
        /// <remarks>
        /// Future drift is maximal allowed block's timestamp difference over adjusted time.
        /// If this difference is greater block won't be accepted.
        /// </remarks>
        /// <param name="time">UNIX timestamp.</param>
        /// <returns>Value of the future drift.</returns>
        public virtual long GetFutureDrift(long time)
        {
            return FutureDriftSeconds;
        }
    }
}