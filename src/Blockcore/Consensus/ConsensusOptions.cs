using Blockcore.Networks;

namespace Blockcore.Consensus
{
    /// <summary>
    /// An extension to <see cref="Consensus"/> to enable additional options to the consensus data.
    /// TODO: Make immutable.
    /// </summary>
    public class ConsensusOptions
    {
        private const int DefaultMaxBlockSigopsCost = 80000;

        /// <summary>
        /// Flag used to detect SegWit transactions.
        /// </summary>
        public const int SerializeTransactionNoWitness = 0x40000000;

        /// <summary>Maximum size for a block in bytes. </summary>
        public uint MaxBlockBaseSize { get; set; } = 1000000;

        /// <summary>The maximum allowed weight for a block, see BIP 141 (network rule)</summary>
        public uint MaxBlockWeight { get; set; } = 4000000;

        /// <summary>Thee minimum fee-rate (fee per kb) a transaction is paying
        /// in order to be included by the miner when mining a block.
        /// </summary>
        public long MinBlockFeeRate { get; set; } = 1000;

        /// <summary>The maximum allowed size for a serialized block, in bytes (only for buffer size limits). </summary>
        public uint MaxBlockSerializedSize { get; set; } = 4000000;

        /// <summary>Scale of witness vs other transaction data. e.g. if set to 4,
        /// then witnesses have 1/4 the weight per byte of other transaction data.
        /// </summary>
        public int WitnessScaleFactor { get; set; } = 4;

        /// <summary>
        /// Changing the default transaction version requires a two step process:
        /// <list type="bullet">
        /// <item>Adapting relay policy by bumping <see cref="MaxStandardVersion"/>,</item>
        /// <item>and then later date bumping the default CURRENT_VERSION at which point both CURRENT_VERSION and
        /// <see cref="MaxStandardVersion"/> will be equal.</item>
        /// </list>
        /// </summary>
        public int MaxStandardVersion { get; set; } = 2;

        /// <summary>The maximum weight for transactions we're willing to relay/mine.</summary>
        public int MaxStandardTxWeight { get; set; } = 400000;

        /// <summary>The maximum allowed number of signature check operations in a block (network rule).</summary>
        public int MaxBlockSigopsCost { get; set; } = DefaultMaxBlockSigopsCost;

        /// <summary>The maximum number of sigops we're willing to relay/mine in a single tx.</summary>
        /// <remarks>
        /// This value is calculated based on <see cref="MaxBlockSigopsCost"/> dived by 5.
        /// </remarks>
        public int MaxStandardTxSigopsCost { get; set; } = DefaultMaxBlockSigopsCost / 5;

        /// <summary>Block Height at which the node should enforce the use of <see cref="EnforcedMinProtocolVersion"/>.
        /// Can be set to zero to indicate that the minimum supported protocol version will not change depending on the block height.</summary>
        public int EnforceMinProtocolVersionAtBlockHeight { get; set; } = 0;

        /// <summary>The minimum protocol version which should be used from block height defined in <see cref="EnforceMinProtocolVersionAtBlockHeight"/></summary>
        public uint? EnforcedMinProtocolVersion { get; set; }
    }

    /// <summary>
    /// Extension to ConsensusOptions for PoS-related parameters.
    ///
    /// TODO: When moving rules to be part of consensus for network, move this class to the appropriate project too.
    /// Doesn't make much sense for it to be in NBitcoin. Also remove the CoinstakeMinConfirmation consts and set CointakeMinConfirmation in Network building.
    /// </summary>
    public class PosConsensusOptions : ConsensusOptions
    {
        /// <summary>Coinstake minimal confirmations softfork activation height for mainnet.</summary>
        public const int CoinstakeMinConfirmationActivationHeightMainnet = 1005000;

        /// <summary>Coinstake minimal confirmations softfork activation height for testnet.</summary>
        public const int CoinstakeMinConfirmationActivationHeightTestnet = 436000;

        /// <summary>
        /// Maximum coinstake serialized size in bytes.
        /// </summary>
        public const int MaxCoinstakeSerializedSize = 1_000_000;

        /// <summary>
        /// Maximum signature serialized size in bytes.
        /// </summary>
        public const int MaxBlockSignatureSerializedSize = 80;

        /// <summary>
        /// Maximum merkle proof serialized size in bytes.
        /// </summary>
        public const int MaxMerkleProofSerializedSize = 512;

        /// <summary>
        /// Gets the minimum confirmations amount required for a coin to be good enough to participate in staking.
        /// </summary>
        /// <param name="height">Block height.</param>
        /// <param name="network">The network.</param>
        public virtual int GetStakeMinConfirmations(int height, Network network)
        {
            // TODO: Is there supposed to be a defined activation height for regtest?
            if (network.NetworkType == NetworkType.Testnet || network.NetworkType == NetworkType.Regtest)
                return height < CoinstakeMinConfirmationActivationHeightTestnet ? 10 : 20;

            return height < CoinstakeMinConfirmationActivationHeightMainnet ? 50 : 500;
        }
    }
}