using System;
using System.Collections.Generic;
using Blockcore.Base.Deployments;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;

namespace Blockcore.Consensus
{
    public interface IConsensus
    {
        /// <summary>
        /// How many blocks should be on top of a block that includes a coinbase transaction until its outputs are considered spendable.
        /// </summary>
        long CoinbaseMaturity { get; set; }

        /// <summary>
        /// Amount of coins mined when a new network is bootstrapped.
        /// Set to <see cref="Money.Zero"/> when there is no premine.
        /// </summary>
        Money PremineReward { get; }

        /// <summary>
        /// The height of the block in which the pre-mined coins should be.
        /// Set to 0 when there is no premine.
        /// </summary>
        long PremineHeight { get; }

        /// <summary>
        /// The reward that goes to the miner when a block is mined using proof-of-work.
        /// </summary>
        Money ProofOfWorkReward { get; }

        /// <summary>
        /// The reward that goes to the miner when a block is mined using proof-of-stake.
        /// </summary>
        Money ProofOfStakeReward { get; }

        /// <summary>
        /// Maximal length of reorganization that the node is willing to accept, or 0 to disable long reorganization protection.
        /// </summary>
        uint MaxReorgLength { get; }

        /// <summary>
        /// The maximum amount of coins in any transaction.
        /// </summary>
        long MaxMoney { get; }

        ConsensusOptions Options { get; set; }

        BuriedDeploymentsArray BuriedDeployments { get; }

        IBIP9DeploymentsArray BIP9Deployments { get; }

        int SubsidyHalvingInterval { get; }

        int MajorityEnforceBlockUpgrade { get; }

        int MajorityRejectBlockOutdated { get; }

        int MajorityWindow { get; }

        uint256 BIP34Hash { get; }

        Target PowLimit { get; }

        TimeSpan TargetTimespan { get; }

        /// <summary>Expected (or target) block time in seconds.</summary>
        TimeSpan TargetSpacing { get; }

        bool PowAllowMinDifficultyBlocks { get; }

        /// <summary>
        /// If <c>true</c> disables checking the next block's difficulty (work required) target on a Proof-Of-Stake network.
        /// <para>
        /// This can be used in tests to enable fast mining of blocks.
        /// </para>
        /// </summary>
        bool PosNoRetargeting { get; }

        /// <summary>
        /// If <c>true</c> disables checking the next block's difficulty (work required) target on a Proof-Of-Work network.
        /// <para>
        /// This can be used in tests to enable fast mining of blocks.
        /// </para>
        /// </summary>
        bool PowNoRetargeting { get; }

        uint256 HashGenesisBlock { get; }

        /// <summary> The minimum amount of work the best chain should have. </summary>
        uint256 MinimumChainWork { get; }

        int MinerConfirmationWindow { get; set; }

        /// <summary>
        /// Specify the BIP44 coin type for this network.
        /// </summary>
        int CoinType { get; }

        BigInteger ProofOfStakeLimit { get; }

        BigInteger ProofOfStakeLimitV2 { get; }

        /// <summary>PoW blocks are not accepted after block with height <see cref="Consensus.LastPOWBlock"/>.</summary>
        int LastPOWBlock { get; set; }

        /// <summary>
        /// This flag will restrict the coinbase in a POS network to be empty.
        /// For legacy POS the coinbase is required to be empty.
        /// </summary>
        /// <remarks>
        /// Some implementations will put extra data in the coinbase (for example the witness commitment)
        /// To allow such data to be in the coinbase we use this flag, a POS network that already has that limitation will use the coinbase input instead.
        /// </remarks>
        bool PosEmptyCoinbase { get; set; }

        /// <summary>
        /// POSv4 emits the time field from the pos kernal calculations.
        /// </summary>
        /// <remarks>
        /// POSv3 uses a few fields to create enough randomness so that the kernal cannot be guessed in advance.
        /// The time field of the utxo that found the stake is one of those parameters.
        /// However POSv4 removes the time form the kernal hash, the prevout utxo provides enough randomness.
        /// </remarks>
        bool PosUseTimeFieldInKernalHash { get; set; }

        /// <summary>A mask for coinstake transaction's timestamp and header's timestamp.</summary>
        /// <remarks>Used to decrease granularity of timestamp. Supposed to be 2^n-1.</remarks>
        public uint ProofOfStakeTimestampMask { get; set; }

        /// <summary>
        /// An indicator whether this is a Proof Of Stake network.
        /// </summary>
        bool IsProofOfStake { get; }

        /// <summary>The default hash to use for assuming valid blocks.</summary>
        uint256 DefaultAssumeValid { get; }

        /// <summary>
        /// A factory that enables overloading base types.
        /// </summary>
        ConsensusFactory ConsensusFactory { get; }

        /// <summary>Group of rules that define a given network.</summary>
        ConsensusRules ConsensusRules { get; }

        /// <summary>Group of mempool validation rules used by the given network.</summary>
        List<Type> MempoolRules { get; set; }
    }
}