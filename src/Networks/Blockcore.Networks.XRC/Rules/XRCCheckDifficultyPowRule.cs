using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Blockcore.Networks.XRC.Consensus;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace Blockcore.Networks.XRC.Rules
{
    public class XRCCheckDifficultyPowRule : HeaderValidationConsensusRule
    {
        private static readonly BigInteger pow256 = BigInteger.ValueOf(2).Pow(256);
        readonly int MedianTimeSpan = 11;
        public override void Run(RuleContext context)
        {
            if (!CheckProofOfWork((XRCBlockHeader)context.ValidationContext.ChainedHeaderToValidate.Header))
                ConsensusErrors.HighHash.Throw();

            Target nextWorkRequired = GetWorkRequired(
                context.ValidationContext.ChainedHeaderToValidate,
                (XRCConsensus)this.Parent.Network.Consensus);

            XRCBlockHeader header = (XRCBlockHeader)context.ValidationContext.ChainedHeaderToValidate.Header;

            // Check proof of work.
            if (header.Bits != nextWorkRequired)
            {
                this.Logger.LogTrace("(-)[BAD_DIFF_BITS]");
                ConsensusErrors.BadDiffBits.Throw();
            }
        }

        private bool CheckProofOfWork(XRCBlockHeader header)
        {
            BigInteger bits = header.Bits.ToBigInteger();
            if ((bits.CompareTo(BigInteger.Zero) <= 0) || (bits.CompareTo(pow256) >= 0))
                return false;

            return header.GetPoWHash() <= header.Bits.ToUInt256();
        }

        public Target GetWorkRequired(ChainedHeader chainedHeaderToValidate, XRCConsensus consensus)
        {
            // Genesis block.
            if (chainedHeaderToValidate.Height == 0)
                return consensus.PowLimit2;

            var XRCConsensusProtocol = (XRCConsensusProtocol)consensus.ConsensusFactory.Protocol;

            //hard fork
            if (chainedHeaderToValidate.Height == XRCConsensusProtocol.PowLimit2Height + 1)
                return consensus.PowLimit;

            //hard fork 2 - DigiShield + X11
            if (chainedHeaderToValidate.Height > XRCConsensusProtocol.PowDigiShieldX11Height)
                return GetWorkRequiredDigiShield(chainedHeaderToValidate, consensus);

            Target proofOfWorkLimit;

            // Hard fork to higher difficulty
            if (chainedHeaderToValidate.Height > XRCConsensusProtocol.PowLimit2Height)
            {
                proofOfWorkLimit = consensus.PowLimit;
            }
            else
            {
                proofOfWorkLimit = consensus.PowLimit2;
            }

            ChainedHeader lastBlock = chainedHeaderToValidate.Previous;
            int height = chainedHeaderToValidate.Height;

            if (lastBlock == null)
                return proofOfWorkLimit;

            long difficultyAdjustmentInterval = GetDifficultyAdjustmentInterval(consensus);

            // Only change once per interval.
            if (height % difficultyAdjustmentInterval != 0)
            {
                if (consensus.PowAllowMinDifficultyBlocks)
                {
                    // Special difficulty rule for testnet:
                    // If the new block's timestamp is more than 2* 10 minutes
                    // then allow mining of a min-difficulty block.
                    if (chainedHeaderToValidate.Header.BlockTime > (lastBlock.Header.BlockTime + TimeSpan.FromTicks(consensus.TargetSpacing.Ticks * 2)))
                        return proofOfWorkLimit;

                    // Return the last non-special-min-difficulty-rules-block.
                    ChainedHeader chainedHeader = lastBlock;
                    while ((chainedHeader.Previous != null) && ((chainedHeader.Height % difficultyAdjustmentInterval) != 0) && (chainedHeader.Header.Bits == proofOfWorkLimit))
                        chainedHeader = chainedHeader.Previous;

                    return chainedHeader.Header.Bits;
                }

                return lastBlock.Header.Bits;
            }

            // Go back by what we want to be 14 days worth of blocks.
            long pastHeight = lastBlock.Height - (difficultyAdjustmentInterval - 1);

            ChainedHeader firstChainedHeader = chainedHeaderToValidate.GetAncestor((int)pastHeight);
            if (firstChainedHeader == null)
                throw new NotSupportedException("Can only calculate work of a full chain");

            if (consensus.PowNoRetargeting)
                return lastBlock.Header.Bits;

            // Limit adjustment step.
            TimeSpan actualTimespan = lastBlock.Header.BlockTime - firstChainedHeader.Header.BlockTime;
            if (actualTimespan < TimeSpan.FromTicks(consensus.TargetTimespan.Ticks / 4))
                actualTimespan = TimeSpan.FromTicks(consensus.TargetTimespan.Ticks / 4);
            if (actualTimespan > TimeSpan.FromTicks(consensus.TargetTimespan.Ticks * 4))
                actualTimespan = TimeSpan.FromTicks(consensus.TargetTimespan.Ticks * 4);

            // Retarget.
            BigInteger newTarget = lastBlock.Header.Bits.ToBigInteger();
            newTarget = newTarget.Multiply(BigInteger.ValueOf((long)actualTimespan.TotalSeconds));
            newTarget = newTarget.Divide(BigInteger.ValueOf((long)consensus.TargetTimespan.TotalSeconds));

            var finalTarget = new Target(newTarget);
            if (finalTarget > proofOfWorkLimit)
                finalTarget = proofOfWorkLimit;

            return finalTarget;
        }

        public Target GetWorkRequiredDigiShield(ChainedHeader chainedHeaderToValidate, XRCConsensus consensus)
        {
            var nAveragingInterval = 10 * 5; // block
            var multiAlgoTargetSpacingV4 = 10 * 60; // seconds
            var nAveragingTargetTimespanV4 = nAveragingInterval * multiAlgoTargetSpacingV4;
            var nMaxAdjustDownV4 = 16;
            var nMaxAdjustUpV4 = 8;
            var nMinActualTimespanV4 = TimeSpan.FromSeconds((double)nAveragingTargetTimespanV4 * (100 - nMaxAdjustUpV4) / 100);
            var nMaxActualTimespanV4 = TimeSpan.FromSeconds((double)nAveragingTargetTimespanV4 * (100 + nMaxAdjustDownV4) / 100);

            var height = chainedHeaderToValidate.Height;
            Target proofOfWorkLimit = consensus.PowLimit2;
            ChainedHeader lastBlock = chainedHeaderToValidate.Previous;
            ChainedHeader firstBlock = chainedHeaderToValidate.GetAncestor(height - nAveragingInterval);

            var XRCConsensusProtocol = (XRCConsensusProtocol)consensus.ConsensusFactory.Protocol;

            if (((height - XRCConsensusProtocol.PowDigiShieldX11Height) <= (nAveragingInterval + this.MedianTimeSpan))
                && (consensus.CoinType == (int)XRCCoinType.CoinTypes.XRCMain))
            {
                return new Target(new uint256("000000000001a61a000000000000000000000000000000000000000000000000"));
            }

            // Limit adjustment step
            // Use medians to prevent time-warp attacks
            TimeSpan nActualTimespan = GetAverageTimePast(lastBlock) - GetAverageTimePast(firstBlock);
            nActualTimespan = TimeSpan.FromSeconds(nAveragingTargetTimespanV4
                                    + ((nActualTimespan.TotalSeconds - nAveragingTargetTimespanV4) / 4));

            if (nActualTimespan < nMinActualTimespanV4)
                nActualTimespan = nMinActualTimespanV4;
            if (nActualTimespan > nMaxActualTimespanV4)
                nActualTimespan = nMaxActualTimespanV4;

            // Retarget.
            BigInteger newTarget = lastBlock.Header.Bits.ToBigInteger();

            newTarget = newTarget.Multiply(BigInteger.ValueOf((long)nActualTimespan.TotalSeconds));
            newTarget = newTarget.Divide(BigInteger.ValueOf(nAveragingTargetTimespanV4));

            var finalTarget = new Target(newTarget);
            if (finalTarget > proofOfWorkLimit)
                finalTarget = proofOfWorkLimit;

            return finalTarget;
        }

        public DateTimeOffset GetAverageTimePast(ChainedHeader chainedHeaderToValidate)
        {
            var median = new List<DateTimeOffset>();

            ChainedHeader chainedHeader = chainedHeaderToValidate;
            for (int i = 0; i < this.MedianTimeSpan && chainedHeader != null; i++, chainedHeader = chainedHeader.Previous)
                median.Add(chainedHeader.Header.BlockTime);

            median.Sort();

            DateTimeOffset firstTimespan = median.First();
            DateTimeOffset lastTimespan = median.Last();
            TimeSpan differenceTimespan = lastTimespan - firstTimespan;
            var timespan = differenceTimespan.TotalSeconds / 2;
            DateTimeOffset averageDateTime = firstTimespan.AddSeconds((long)timespan);

            return averageDateTime;
        }

        private long GetDifficultyAdjustmentInterval(IConsensus consensus)
        {
            return (long)consensus.TargetTimespan.TotalSeconds / (long)consensus.TargetSpacing.TotalSeconds;
        }
    }
}