using System;
using System.Runtime.CompilerServices;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace Blockcore.Networks.X1.Consensus
{
    /// <inheritdoc />
    public class X1ConsensusOptions : PosConsensusOptions
    {
        private const int PosPowRatchetIsActiveHeightInvalid = -1;

        /// <summary>
        /// The block height (inclusive), where the PosPowRatchet algorithm starts, on TestNet.
        /// </summary>
        private const int PosPowRatchetIsActiveHeightTestNet = 240;

        /// <summary>
        /// The block height (inclusive), where the PosPowRatchet algorithm starts, on MainNet.
        /// </summary>
        private const int PosPowRatchetIsActiveHeightMainNet = 163300; // ~19/12/2020 04:00h

        private readonly Network currentNetwork;

        public X1ConsensusOptions(Network network)
        {
            this.currentNetwork = network;
        }

        /// <inheritdoc />
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            // StakeMinConfirmations must equal MaxReorgLength so that nobody can stake in isolation and then force a reorg
            return (int)network.Consensus.MaxReorgLength;
        }

        /// <inheritdoc />
        public bool IsAlgorithmAllowed(bool isProofOfStake, int newBlockHeight)
        {
            if (this.currentNetwork.NetworkType == NetworkType.Mainnet)
            {
                if (newBlockHeight < PosPowRatchetIsActiveHeightMainNet)
                    return true;

                bool isPosHeight = newBlockHeight % 2 == 0; // for X1, even block heights must be Proof-of-Stake

                if (isProofOfStake && isPosHeight)
                    return true;

                if (!isProofOfStake && !isPosHeight)
                    return true;

                return false;
            }

            if (this.currentNetwork.NetworkType == NetworkType.Testnet)
            {
                if (newBlockHeight < PosPowRatchetIsActiveHeightTestNet)
                    return true;

                bool isPosHeight = newBlockHeight % 2 == 0; // for X1, even block heights must be Proof-of-Stake

                if (isProofOfStake && isPosHeight)
                    return true;

                if (!isProofOfStake && !isPosHeight)
                    return true;

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public bool IsPosPowRatchetActiveAtHeight(int chainTipHeight)
        {
            if (this.currentNetwork.NetworkType == NetworkType.Testnet)
            {
                if (chainTipHeight >= PosPowRatchetIsActiveHeightTestNet)
                    return true;
            }
            if (this.currentNetwork.NetworkType == NetworkType.Mainnet)
            {
                if (chainTipHeight >= PosPowRatchetIsActiveHeightMainNet)
                    return true;
            }

            return false;
        }

        double GetTargetTimespanTotalSeconds(int height)
        {
            return GetTargetSpacingTotalSeconds(height) * 338;
        }

       
        double GetTargetSpacingTotalSeconds(int height)
        {
            if (this.currentNetwork.NetworkType != NetworkType.Mainnet)
                return this.currentNetwork.Consensus.TargetSpacing.TotalSeconds; // 256 seconds

            // X1 Main
            if (height < 165740)
                return this.currentNetwork.Consensus.TargetSpacing.TotalSeconds; // 256 seconds

            return TimeSpan.FromMinutes(10).TotalSeconds; // 600 seconds
        }


        private readonly object lockObj = new object();

        public Target GetNextTargetRequired(ChainedHeader currentChainTip, bool isChainTipProofOfStake, IConsensus consensus, bool isTargetRequestedForProofOfStake)
        {
            if (currentChainTip == null)
                throw new ArgumentNullException(nameof(currentChainTip));

            if (consensus == null)
                throw new ArgumentNullException(nameof(consensus));

            lock (this.lockObj)
            {
                // Precondition and sanity checks. Strict precondition checks here allow for more robust and faster code
                // in the actual logic. Fast code is necessary here, because this code is called often, and it iterates
                // over many headers to calculate the block interval averages. Therefore, the even/odd convention for PoS/PoW
                // blocks is double-checked here as well, because it allows us not to use IStakeChain which would be slow when 
                // iterating over many headers.

                // This code must not be called before the ratchet has been active for at least 4 blocks.
                if (!IsPosPowRatchetActiveAtHeight(currentChainTip.Height - 4))
                    throw new InvalidOperationException($"Precondition failed: PosPowRatchet has not been active 4 blocks before the current tip height of {currentChainTip.Height}.");

                ChainedHeader lastPowPosBlock = currentChainTip;

                // The caller passes an argument, whether a PoS or PoW Target is requested.
                if (isTargetRequestedForProofOfStake)
                {
                    // Starting point will be the last PoS block.
                    if (!isChainTipProofOfStake)
                    {
                        // The previous block is guaranteed to be a PoS block, due to the offset of 2 to the ratchet activation height
                        // and the precondition check when calling this from StakeValidator.
                        lastPowPosBlock = lastPowPosBlock.Previous;
                    }

                    // We are passing in a PoS block!
                    return GetNextPosTargetRequired(lastPowPosBlock, consensus);
                }

                // Starting point will be the last PoW block.
                if (isChainTipProofOfStake)
                {
                    // The previous block is guaranteed to be a PoW block, due to the offset of 2 to the ratchet activation height
                    // and the precondition check when calling this from StakeValidator.
                    lastPowPosBlock = lastPowPosBlock.Previous;
                }

                // We are passing in a PoW block!
                return GetNextWorkRequired(lastPowPosBlock, consensus);

            }
        }

        private Target GetNextWorkRequired(ChainedHeader lastPowBlock, IConsensus consensus)
        {
            int difficultyAdjustmentInterval = (int)(GetTargetTimespanTotalSeconds(lastPowBlock.Height) / GetTargetSpacingTotalSeconds(lastPowBlock.Height));

            // Only change once per difficulty adjustment interval
            if ((lastPowBlock.Height + 1) % difficultyAdjustmentInterval != 0)
            {
                if (consensus.PowAllowMinDifficultyBlocks)
                {
                    // Special difficulty rule for testnet:
                    // If the new block's timestamp is more than 2 * TargetSpacing.TotalSeconds,
                    // then allow mining of a min-difficulty block.
                    if (lastPowBlock.Header.Time > lastPowBlock.Header.Time + GetTargetSpacingTotalSeconds(lastPowBlock.Height) * 2)
                        return consensus.PowLimit;
                    else
                    {
                        // Return the last non-special-min-difficulty-rules-block
                        ChainedHeader pindex = lastPowBlock;
                        while (pindex.Previous != null && pindex.Height % difficultyAdjustmentInterval != 0 && pindex.Header.Bits == consensus.PowLimit)
                            pindex = pindex.Previous;
                        return pindex.Header.Bits;
                    }
                }

                // Not changing the Target means we return the previous PoW Target.
                return lastPowBlock.Header.Bits;
            }

            // We'll also not adjust the difficulty, if the ratchet wasn't active at least 2x difficultyAdjustmentInterval + 4 blocks.
            if (lastPowBlock.Height < GetPosPowRatchetIsActiveHeight() + 2 * difficultyAdjustmentInterval + 4)
                return lastPowBlock.Header.Bits;

            // Define the amount of PoW blocks used to calculate the average, and for the sake of logic,
            // don't repeat Bitcoin's off-by one error.
            var amountOfPoWBlocks = difficultyAdjustmentInterval; // 338

            ChainedHeader powBlockIterator = lastPowBlock;
            var powBlockCount = 0;
            var posGaps = 0u;
            var powIntervalsIncPosSum = 0u;

            while (powBlockCount < amountOfPoWBlocks)
            {

                // we are sure this is a Pos block because of the precondition checks, which previous blocks have already passed
                ChainedHeader intermediatePosBlock = powBlockIterator.Previous;

                // we are sure this is a Pow block because of the precondition checks, which previous blocks have already passed
                ChainedHeader prevLastPowBlock = intermediatePosBlock.Previous;

                // the time in seconds the intermediate PoS block has used, which is must be hidden for the calculation
                var posGapSeconds = intermediatePosBlock.Header.Time - prevLastPowBlock.Header.Time;
                posGaps += posGapSeconds;

                var grossPowSeconds = powBlockIterator.Header.Time - prevLastPowBlock.Header.Time;
                powIntervalsIncPosSum += grossPowSeconds;

                // update the iterator
                powBlockIterator = prevLastPowBlock;

                // update the counter
                powBlockCount++;
            }

            var powActualTimeSpanIncPos = lastPowBlock.Header.Time - powBlockIterator.Header.Time;

            Assert(powBlockCount == amountOfPoWBlocks);
            Assert(powIntervalsIncPosSum == powActualTimeSpanIncPos);

            var firstPowBlockHeaderTimeExceptGaps = lastPowBlock.Header.Time - powActualTimeSpanIncPos + posGaps;
            return CalculatePoWTarget(lastPowBlock, firstPowBlockHeaderTimeExceptGaps, consensus);
        }

        private Target GetNextPosTargetRequired(ChainedHeader lastPosBlock, IConsensus consensus)
        {
            // We'll need to go back 2 blocks, to calculate the time delta. We can only do that if the ratchet
            // was active 2 blocks before the current lastPosBlock. So if that's not possible, we simply return 
            // the previous Target. Due to the precondition checks, we know it's a valid PoS Target.
            if (!IsPosPowRatchetActiveAtHeight(lastPosBlock.Height - 2) || consensus.PosNoRetargeting)
            {
                return lastPosBlock.Header.Bits;
            }

            // we are sure this is a PoW block because of the precondition checks, which previous blocks have already passed
            ChainedHeader intermediatePowBlock = lastPosBlock.Previous;

            // we are sure this is a PoS block because of the precondition checks, which previous blocks have already passed
            ChainedHeader prevLastPosBlock = intermediatePowBlock.Previous;

            // the time in seconds the intermediate PoS block has used, which is must be hidden for the calculation
            var powGapSeconds = intermediatePowBlock.Header.Time - prevLastPosBlock.Header.Time;

            // add the powGapSeconds to the timestamp of the prevLastPosBlock, to compensate the time it took to create the PoW block
            var adjustedPrevLastPowPosBlockTime = prevLastPosBlock.Header.Time + powGapSeconds;

            // pass in adjustedPrevLastPowPosBlockTime instead of the timestamp of the second block, and continue as normal
            return CalculatePosRetarget(lastPosBlock.Header.Time, lastPosBlock.Header.Bits, adjustedPrevLastPowPosBlockTime, consensus.ProofOfStakeLimitV2, lastPosBlock.Height);
        }

        private Target CalculatePosRetarget(uint lastBlockTime, Target lastBlockTarget, uint previousBlockTime, BigInteger targetLimit, int height)
        {
            uint targetSpacing = (uint)GetTargetSpacingTotalSeconds(height); // = 256s or 10 minutes after hf
            uint actualSpacing = lastBlockTime - previousBlockTime; // this is never 0 or negative because that's a consensus rule

            // Limit the adjustment step by capping input values that are far from the average.
            if (actualSpacing > targetSpacing * 4) // if the spacing was > 1024 seconds, pretend is was 1024 seconds
                actualSpacing = targetSpacing * 4;
            if (actualSpacing < targetSpacing / 4) // if the spacing was < 64 seconds, pretend is was 64 seconds
                actualSpacing = targetSpacing / 4;

            BigInteger nextTarget = lastBlockTarget.ToBigInteger();

            // To reduce the impact of randomness, the actualSpacing's weight is reduced to 1/32th (instead of 1/2). This creates
            // similar results like using 32-period average.
            // The problem with random spacing values is that they frequently lead to difficult adjustments in the wrong direction,
            // when the sample size is as low as 1.
            // The results with 1/2 were: PoS block ETA seconds: Average: 351, Median: 165. But average and median should have been 256 seconds.
            long numerator = 31 * targetSpacing + actualSpacing;
            long denominator = 32 * targetSpacing;
            nextTarget = nextTarget.Multiply(BigInteger.ValueOf(numerator));
            nextTarget = nextTarget.Divide(BigInteger.ValueOf(denominator));

            if (nextTarget.CompareTo(BigInteger.Zero) <= 0 || nextTarget.CompareTo(targetLimit) >= 1)
                nextTarget = targetLimit;

            return new Target(nextTarget);
        }

        private Target CalculatePoWTarget(ChainedHeader lastPowBlock, uint nFirstBlockTime, IConsensus consensus)
        {
            // This is used in tests to allow quickly mining blocks.
            if (consensus.PowNoRetargeting)
            {
                return lastPowBlock.Header.Bits;
            }

            int height = lastPowBlock.Height;

            // Limit adjustment step
            long nActualTimespan = lastPowBlock.Header.Time - nFirstBlockTime;
            if (nActualTimespan < GetTargetTimespanTotalSeconds(height) / 4)
                nActualTimespan = (uint)GetTargetTimespanTotalSeconds(height) / 4;
            if (nActualTimespan > GetTargetTimespanTotalSeconds(height) * 4)
                nActualTimespan = (uint)GetTargetTimespanTotalSeconds(height) * 4;

            // Retarget
            var bnNew = lastPowBlock.Header.Bits.ToBigInteger();
            bnNew = bnNew.Multiply(BigInteger.ValueOf(nActualTimespan));
            bnNew = bnNew.Divide(BigInteger.ValueOf((long)GetTargetTimespanTotalSeconds(height)));

            var finalTarget = new Target(bnNew);
            if (finalTarget > consensus.PowLimit)
                finalTarget = consensus.PowLimit;

            return finalTarget;
        }

        private int GetPosPowRatchetIsActiveHeight()
        {
            switch (this.currentNetwork.NetworkType)
            {
                case NetworkType.Mainnet:
                    return PosPowRatchetIsActiveHeightMainNet;
                case NetworkType.Testnet:
                    return PosPowRatchetIsActiveHeightTestNet;
                case NetworkType.Regtest:
                    return PosPowRatchetIsActiveHeightInvalid;
            }

            return PosPowRatchetIsActiveHeightInvalid;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void Assert(bool condition, [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
        {
            if (!condition)
            {
                throw new Exception($"{nameof(X1ConsensusOptions)} - assert failed! Caller {caller}, line: {line}.");
            }
        }
    }
}