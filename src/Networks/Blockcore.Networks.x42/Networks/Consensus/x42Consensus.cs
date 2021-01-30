using System;
using System.Collections.Generic;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace Blockcore.Networks.x42.Networks.Consensus
{
    public class x42Consensus : IConsensus
    {
        /// <summary>
        /// How many blocks should be on top of a block that includes a coinbase transaction until its outputs are considered spendable.
        /// </summary>
        public long CoinbaseMaturity { get; set; }

        /// <summary>
        /// Amount of coins mined when a new network is bootstrapped.
        /// Set to <see cref="Money.Zero"/> when there is no premine.
        /// </summary>
        public Money PremineReward { get; }

        /// <summary>
        /// The height of the block in which the pre-mined coins should be.
        /// Set to 0 when there is no premine.
        /// </summary>
        public long PremineHeight { get; }

        /// <summary>
        /// The reward that goes to the miner when a block is mined using proof-of-work.
        /// </summary>
        public Money ProofOfWorkReward { get; }

        /// <summary>
        /// The reward that goes to the miner when a block is mined using proof-of-stake.
        /// </summary>
        public Money ProofOfStakeReward { get; }

        /// <summary>
        /// Maximal length of reorganization that the node is willing to accept, or 0 to disable long reorganization protection.
        /// </summary>
        public uint MaxReorgLength { get; }

        /// <summary>
        /// The maximum amount of coins in any transaction.
        /// </summary>
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

        /// <summary>Expected (or target) block time in seconds.</summary>
        public TimeSpan TargetSpacing { get; }

        public bool PowAllowMinDifficultyBlocks { get; }

        /// <summary>
        /// If <c>true</c> disables checking the next block's difficulty (work required) target on a Proof-Of-Stake network.
        /// <para>
        /// This can be used in tests to enable fast mining of blocks.
        /// </para>
        /// </summary>
        public bool PosNoRetargeting { get; }

        /// <summary>
        /// If <c>true</c> disables checking the next block's difficulty (work required) target on a Proof-Of-Work network.
        /// <para>
        /// This can be used in tests to enable fast mining of blocks.
        /// </para>
        /// </summary>
        public bool PowNoRetargeting { get; }

        public uint256 HashGenesisBlock { get; }

        /// <summary> The minimum amount of work the best chain should have. </summary>
        public uint256 MinimumChainWork { get; }

        public int MinerConfirmationWindow { get; set; }

        /// <summary>
        /// Specify the BIP44 coin type for this network.
        /// </summary>
        public int CoinType { get; }

        public BigInteger ProofOfStakeLimit { get; }

        public BigInteger ProofOfStakeLimitV2 { get; }

        /// <summary>PoW blocks are not accepted after block with height <see cref="Consensus.LastPOWBlock"/>.</summary>
        public int LastPOWBlock { get; set; }

        /// <summary>
        /// This flag will restrict the coinbase in a POS network to be empty.
        /// For legacy POS the coinbase is required to be empty.
        /// </summary>
        /// <remarks>
        /// Some implementations will put extra data in the coinbase (for example the witness commitment)
        /// To allow such data to be in the coinbase we use this flag, a POS network that already has that limitation will use the coinbase input instead.
        /// </remarks>
        public bool PosEmptyCoinbase { get; set; }

        /// <summary>
        /// POSv4 emits the time field from the pos kernal calculations.
        /// </summary>
        /// <remarks>
        /// POSv3 uses a few fields to create enough randomness so that the kernal cannot be guessed in advance.
        /// The time field of the utxo that found the stake is one of those parameters.
        /// However POSv4 removes the time form the kernal hash, the prevout utxo provides enough randomness.
        /// </remarks>
        public bool PosUseTimeFieldInKernalHash { get; set; }

        /// <summary>A mask for coinstake transaction's timestamp and header's timestamp.</summary>
        /// <remarks>Used to decrease granularity of timestamp. Supposed to be 2^n-1.</remarks>
        public uint ProofOfStakeTimestampMask { get; set; }

        /// <summary>
        /// An indicator whether this is a Proof Of Stake network.
        /// </summary>
        public bool IsProofOfStake { get; }

        /// <summary>The default hash to use for assuming valid blocks.</summary>
        public uint256 DefaultAssumeValid { get; }

        /// <summary>
        /// A factory that enables overloading base types.
        /// </summary>
        public ConsensusFactory ConsensusFactory { get; }

        /// <summary>Group of rules that define a given network.</summary>
        public ConsensusRules ConsensusRules { get; }

        /// <summary>Group of mempool validation rules used by the given network.</summary>
        public List<Type> MempoolRules { get; set; }

        public Money ProofOfStakeRewardAfterSubsidyLimit { get; }

        public long SubsidyLimit { get; }

        /// <inheritdoc />
        public Money LastProofOfStakeRewardHeight { get; }

        public Money MinOpReturnFee { get; }

        public x42Consensus(
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
            BigInteger proofOfStakeLimitV2,
            Money proofOfStakeReward,
            Money proofOfStakeRewardAfterSubsidyLimit,
            long subsidyLimit,
            Money lastProofOfStakeRewardHeight,
            Money minOpReturnFee,
            bool posEmptyCoinbase,
            uint proofOfStakeTimestampMask
            )
        {
            this.CoinbaseMaturity = coinbaseMaturity;
            this.PremineReward = premineReward;
            this.PremineHeight = premineHeight;
            this.ProofOfWorkReward = proofOfWorkReward;
            this.ProofOfStakeReward = proofOfStakeReward;
            this.TargetTimespan = targetTimespan;
            this.TargetSpacing = targetSpacing;
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
            this.PowAllowMinDifficultyBlocks = powAllowMinDifficultyBlocks;
            this.PosNoRetargeting = posNoRetargeting;
            this.PowNoRetargeting = powNoRetargeting;
            this.HashGenesisBlock = hashGenesisBlock;
            this.MinimumChainWork = minimumChainWork;
            this.MinerConfirmationWindow = minerConfirmationWindow;
            this.CoinType = coinType;
            this.ProofOfStakeLimitV2 = proofOfStakeLimitV2;
            this.LastPOWBlock = lastPowBlock;
            this.IsProofOfStake = isProofOfStake;
            this.DefaultAssumeValid = defaultAssumeValid;
            this.ConsensusFactory = consensusFactory;
            this.ProofOfStakeRewardAfterSubsidyLimit = proofOfStakeRewardAfterSubsidyLimit;
            this.SubsidyLimit = subsidyLimit;
            this.LastProofOfStakeRewardHeight = lastProofOfStakeRewardHeight;
            this.MinOpReturnFee = minOpReturnFee;
            this.ConsensusRules = new ConsensusRules();
            this.MempoolRules = new List<Type>();
            this.PosEmptyCoinbase = posEmptyCoinbase;
            this.ProofOfStakeTimestampMask = proofOfStakeTimestampMask;
        }
    }
}