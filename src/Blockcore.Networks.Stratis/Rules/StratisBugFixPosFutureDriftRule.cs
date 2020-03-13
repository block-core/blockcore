using Blockcore.Features.Consensus.Rules.CommonRules;

namespace Blockcore.Networks.Stratis.Rules
{
    /// <summary>
    /// A rule that will verify the block time drift is according to the PoS consensus rules for the <see cref="StratisMain"/> network (and its test networks).
    /// New networks must use the <see cref="PosFutureDriftRule"/>.
    /// </summary>
    public class StratisBugFixPosFutureDriftRule : PosFutureDriftRule
    {
        /// <summary>Drifting Bug Fix, hardfork on Sat, 19 Nov 2016 00:00:00 GMT.</summary>
        public const long DriftingBugFixTimestamp = 1479513600;

        /// <summary>Old future drift in seconds before the hardfork.</summary>
        private const int BugFutureDriftSeconds = 128 * 60 * 60;

        /// <summary>
        /// Gets future drift for the provided timestamp.
        /// </summary>
        /// <remarks>
        /// Future drift is maximal allowed block's timestamp difference over adjusted time.
        /// If this difference is greater block won't be accepted.
        /// </remarks>
        /// <param name="time">UNIX timestamp.</param>
        /// <returns>Value of the future drift.</returns>
        public override long GetFutureDrift(long time)
        {
            return IsDriftReduced(time) ? FutureDriftSeconds : BugFutureDriftSeconds;
        }

        /// <summary>
        /// Checks whether the future drift should be reduced after provided timestamp.
        /// </summary>
        /// <param name="time">UNIX timestamp.</param>
        /// <returns><c>true</c> if for this timestamp future drift should be reduced, <c>false</c> otherwise.</returns>
        private static bool IsDriftReduced(long time)
        {
            // This is a specific Stratis bug fix where the blockchain drifted 24 hour ahead as the protocol allowed that.
            // The protocol was fixed but historical blocks are still effected.
            return time > DriftingBugFixTimestamp;
        }
    }
}