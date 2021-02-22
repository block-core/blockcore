using System;
using System.Collections.Generic;
using Blockcore.Base.Deployments;
using Blockcore.Consensus.Chain;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace Blockcore.Consensus
{
    public class Consensus : IConsensus
    {
        /// <inheritdoc />
        public long CoinbaseMaturity { get; set; }

        /// <inheritdoc />
        public Money PremineReward { get; }

        /// <inheritdoc />
        public long PremineHeight { get; }

        /// <inheritdoc />
        public Money ProofOfWorkReward { get; }

        /// <inheritdoc />
        public Money ProofOfStakeReward { get; }

        /// <inheritdoc />
        public uint MaxReorgLength { get; private set; }

        /// <inheritdoc />
        public long MaxMoney { get; }

        public ConsensusOptions Options { get; set; }

        public BuriedDeploymentsArray BuriedDeployments { get; }

        public IBIP9DeploymentsArray BIP9Deployments { get; }

        public int SubsidyHalvingInterval { get; }

        public int MajorityEnforceBlockUpgrade { get; }

        public int MajorityRejectBlockOutdated { get; }

        public int MajorityWindow { get; }

        public uint256 BIP34Hash { get; }

        public Target PowLimit { get; }

        public TimeSpan TargetTimespan { get; }

        public TimeSpan TargetSpacing { get; }

        public bool PowAllowMinDifficultyBlocks { get; }

        /// <inheritdoc />
        public bool PosNoRetargeting { get; }

        /// <inheritdoc />
        public bool PowNoRetargeting { get; }

        public uint256 HashGenesisBlock { get; }

        /// <inheritdoc />
        public uint256 MinimumChainWork { get; }

        public int MinerConfirmationWindow { get; set; }

        /// <inheritdoc />
        public int CoinType { get; }

        public BigInteger ProofOfStakeLimit { get; }

        public BigInteger ProofOfStakeLimitV2 { get; }

        /// <inheritdoc />
        public int LastPOWBlock { get; set; }

        /// <inheritdoc />
        public bool IsProofOfStake { get; }

        /// <inheritdoc />
        public bool PosEmptyCoinbase { get; set; }

        /// <inheritdoc />
        public bool PosUseTimeFieldInKernalHash { get; set; }

        /// <inheritdoc />
        public uint ProofOfStakeTimestampMask { get; set; }

        /// <inheritdoc />
        public uint256 DefaultAssumeValid { get; }

        /// <inheritdoc />
        public ConsensusFactory ConsensusFactory { get; }

        /// <inheritdoc />
        public ConsensusRules ConsensusRules { get; }

        /// <inheritdoc />
        public List<Type> MempoolRules { get; set; }

        public Consensus(
            ConsensusFactory consensusFactory,
            ConsensusOptions consensusOptions,
            int coinType,
            uint256 hashGenesisBlock,
            int subsidyHalvingInterval,
            int majorityEnforceBlockUpgrade,
            int majorityRejectBlockOutdated,
            int majorityWindow,
            BuriedDeploymentsArray buriedDeployments,
            IBIP9DeploymentsArray bip9Deployments,
            uint256 bip34Hash,
            int minerConfirmationWindow,
            uint maxReorgLength,
            uint256 defaultAssumeValid,
            long maxMoney,
            long coinbaseMaturity,
            long premineHeight,
            Money premineReward,
            Money proofOfWorkReward,
            TimeSpan targetTimespan,
            TimeSpan targetSpacing,
            bool powAllowMinDifficultyBlocks,
            bool posNoRetargeting,
            bool powNoRetargeting,
            Target powLimit,
            uint256 minimumChainWork,
            bool isProofOfStake,
            int lastPowBlock,
            BigInteger proofOfStakeLimit,
            BigInteger proofOfStakeLimitV2,
            Money proofOfStakeReward,
            uint proofOfStakeTimestampMask)
        {
            this.CoinbaseMaturity = coinbaseMaturity;
            this.PremineReward = premineReward;
            this.PremineHeight = premineHeight;
            this.ProofOfWorkReward = proofOfWorkReward;
            this.ProofOfStakeReward = proofOfStakeReward;
            this.MaxReorgLength = maxReorgLength;
            this.MaxMoney = maxMoney;
            this.Options = consensusOptions;
            this.BuriedDeployments = buriedDeployments;
            this.BIP9Deployments = bip9Deployments;
            this.SubsidyHalvingInterval = subsidyHalvingInterval;
            this.MajorityEnforceBlockUpgrade = majorityEnforceBlockUpgrade;
            this.MajorityRejectBlockOutdated = majorityRejectBlockOutdated;
            this.MajorityWindow = majorityWindow;
            this.BIP34Hash = bip34Hash;
            this.PowLimit = powLimit;
            this.TargetTimespan = targetTimespan;
            this.TargetSpacing = targetSpacing;
            this.PowAllowMinDifficultyBlocks = powAllowMinDifficultyBlocks;
            this.PosNoRetargeting = posNoRetargeting;
            this.PowNoRetargeting = powNoRetargeting;
            this.HashGenesisBlock = hashGenesisBlock;
            this.MinimumChainWork = minimumChainWork;
            this.MinerConfirmationWindow = minerConfirmationWindow;
            this.CoinType = coinType;
            this.ProofOfStakeLimit = proofOfStakeLimit;
            this.ProofOfStakeLimitV2 = proofOfStakeLimitV2;
            this.LastPOWBlock = lastPowBlock;
            this.IsProofOfStake = isProofOfStake;
            this.DefaultAssumeValid = defaultAssumeValid;
            this.ConsensusFactory = consensusFactory;
            this.ConsensusRules = new ConsensusRules();
            this.MempoolRules = new List<Type>();
            this.ProofOfStakeTimestampMask = proofOfStakeTimestampMask;
        }

        /// <summary>
        /// Gets the proof of work target for a given entry in the chain.
        /// </summary>
        /// <param name="chainedHeader">The header for which to calculate the required work.</param>
        /// <returns>The target proof of work.</returns>
        public Target GetWorkRequired(ChainedHeader chainedHeader)
        {
            // Genesis block.
            if (chainedHeader.Height == 0)
                return this.PowLimit;

            Target proofOfWorkLimit = this.PowLimit;
            ChainedHeader lastBlock = chainedHeader.Previous;
            int height = chainedHeader.Height;

            if (lastBlock == null)
                return proofOfWorkLimit;

            // Calculate the difficulty adjustment interval in blocks
            long difficultyAdjustmentInterval = (long)this.TargetTimespan.TotalSeconds / (long)this.TargetSpacing.TotalSeconds;

            // Only change once per interval.
            if ((height) % difficultyAdjustmentInterval != 0)
            {
                if (this.PowAllowMinDifficultyBlocks)
                {
                    // Special difficulty rule for testnet:
                    // If the new block's timestamp is more than 2* 10 minutes
                    // then allow mining of a min-difficulty block.
                    if (chainedHeader.Header.BlockTime > (lastBlock.Header.BlockTime + TimeSpan.FromTicks(this.TargetSpacing.Ticks * 2)))
                        return proofOfWorkLimit;

                    // Return the last non-special-min-difficulty-rules-block.
                    ChainedHeader lastChainedHeader = lastBlock;
                    while ((lastChainedHeader.Previous != null) && ((lastChainedHeader.Height % difficultyAdjustmentInterval) != 0) && (lastChainedHeader.Header.Bits == proofOfWorkLimit))
                        lastChainedHeader = lastChainedHeader.Previous;

                    return lastChainedHeader.Header.Bits;
                }

                return lastBlock.Header.Bits;
            }

            // Go back by what we want to be 14 days worth of blocks.
            long pastHeight = lastBlock.Height - (difficultyAdjustmentInterval - 1);

            ChainedHeader firstChainedHeader = chainedHeader.GetAncestor((int)pastHeight);
            if (firstChainedHeader == null)
                throw new NotSupportedException("Can only calculate work of a full chain");

            if (this.PowNoRetargeting)
                return lastBlock.Header.Bits;

            // Limit adjustment step.
            TimeSpan actualTimespan = lastBlock.Header.BlockTime - firstChainedHeader.Header.BlockTime;
            if (actualTimespan < TimeSpan.FromTicks(this.TargetTimespan.Ticks / 4))
                actualTimespan = TimeSpan.FromTicks(this.TargetTimespan.Ticks / 4);
            if (actualTimespan > TimeSpan.FromTicks(this.TargetTimespan.Ticks * 4))
                actualTimespan = TimeSpan.FromTicks(this.TargetTimespan.Ticks * 4);

            // Retarget.
            BigInteger newTarget = lastBlock.Header.Bits.ToBigInteger();
            newTarget = newTarget.Multiply(BigInteger.ValueOf((long)actualTimespan.TotalSeconds));
            newTarget = newTarget.Divide(BigInteger.ValueOf((long)this.TargetTimespan.TotalSeconds));

            var finalTarget = new Target(newTarget);
            if (finalTarget > proofOfWorkLimit)
                finalTarget = proofOfWorkLimit;

            return finalTarget;
        }
    }
}