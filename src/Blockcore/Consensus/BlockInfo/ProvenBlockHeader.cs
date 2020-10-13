using System;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;

namespace Blockcore.Consensus.BlockInfo
{
    /// <summary>
    /// <para>
    /// Extension to an existing <see cref="BlockHeader"/> which is used in PoS to prevent attacker from constructing
    /// a fake chain of headers that has more work than the valid chain and attacking a node.
    /// </para>
    /// <para>
    /// Proven header prevents such an attack by including additional information that can be validated and confirmed whether
    /// the header is fake or real.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Additional information included into proven header:
    /// </para>
    /// <para>
    /// Block header signature (<see cref="Signature"/>), which is signed with the private key which corresponds to
    /// coinstake's second output's public key.
    /// </para>
    /// <para>
    /// Coinstake transaction (<see cref="Coinstake"/>).
    /// </para>
    /// <para>
    /// Merkle proof (<see cref="MerkleProof"/>) that proves the coinstake tx is included in a block that is being represented by the provided header.
    /// </para>
    /// </remarks>
    public class ProvenBlockHeader : IBitcoinSerializable
    {
        /// <summary>
        /// Proof Of Stake header.
        /// </summary>
        private PosBlockHeader posBlockHeader;

        /// <summary>
        /// Proof Of Stake header.
        /// </summary>
        public PosBlockHeader PosBlockHeader => this.posBlockHeader;

        /// <summary>
        /// Coinstake transaction.
        /// </summary>
        private Transaction coinstake;

        /// <summary>
        /// Gets coinstake transaction.
        /// </summary>
        public Transaction Coinstake => this.coinstake;

        /// <summary>
        /// Merkle proof that proves the coinstake tx is included in a block that is being represented by the provided header.
        /// </summary>
        private PartialMerkleTree merkleProof;

        /// <summary>
        /// Gets merkle proof that proves the coinstake tx is included in a block that is being represented by the provided header.
        /// </summary>
        public PartialMerkleTree MerkleProof => this.merkleProof;

        /// <summary>
        /// Block header signature which is signed with the private key which corresponds to
        /// coinstake's second output's public key.
        /// </summary>
        private BlockSignature signature;

        /// <summary>
        /// Gets block header signature which is signed with the private key which corresponds to
        /// coinstake's second output's public key.
        /// </summary>
        public BlockSignature Signature => this.signature;

        public long PosHeaderSize { get; protected set; }

        /// <summary>Gets the size of the merkle proof in bytes, the header must be serialized or deserialized for this property to be set.</summary>
        public long MerkleProofSize { get; protected set; }

        /// <summary>Gets the size of the signature in bytes, the header must be serialized or deserialized for this property to be set.</summary>
        public long SignatureSize { get; protected set; }

        /// <summary>Gets the size of the coinstake in bytes, the header must be serialized or deserialized for this property to be set.</summary>
        public long CoinstakeSize { get; protected set; }

        /// <summary>Gets the total header size - including the <see cref="BlockHeader.Size"/> - in bytes. <see cref="ProvenBlockHeader"/> must be serialized or deserialized for this property to be set.</summary>
        public long HeaderSize => this.PosHeaderSize + this.MerkleProofSize + this.SignatureSize + this.CoinstakeSize;

        /// <summary>
        /// Gets or sets the stake modifier v2.
        /// </summary>
        /// <value>
        /// The stake modifier v2.
        /// </value>
        /// <remarks>This property is used only in memory, it is not persisted to disk not sent over any Payloads.</remarks>
        public uint256 StakeModifierV2 { get; set; }

        public ProvenBlockHeader()
        {
        }

        public ProvenBlockHeader(PosBlock block, PosBlockHeader posBlockHeader)
        {
            if (block == null) throw new ArgumentNullException(nameof(block));

            // Copy block header properties.
            this.posBlockHeader = posBlockHeader;
            this.posBlockHeader.HashPrevBlock = block.Header.HashPrevBlock;
            this.posBlockHeader.HashMerkleRoot = block.Header.HashMerkleRoot;
            this.posBlockHeader.Time = block.Header.Time;
            this.posBlockHeader.Bits = block.Header.Bits;
            this.posBlockHeader.Nonce = block.Header.Nonce;
            this.posBlockHeader.Version = block.Header.Version;
            this.posBlockHeader.ProvenBlockHeader = this;

            this.signature = block.BlockSignature;
            this.coinstake = block.GetProtocolTransaction();
            this.merkleProof = new MerkleBlock(block, new[] { this.coinstake.GetHash() }).PartialMerkleTree;
        }

        /// <inheritdoc />
        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.posBlockHeader);
            long prev = stream.ProcessedBytes;
            if (!stream.Serializing)
                this.posBlockHeader.ProvenBlockHeader = this;

            stream.ReadWrite(ref this.merkleProof);
            this.MerkleProofSize = stream.ProcessedBytes - prev;

            prev = stream.ProcessedBytes;
            stream.ReadWrite(ref this.signature);
            this.SignatureSize = stream.ProcessedBytes - prev;

            prev = stream.ProcessedBytes;
            stream.ReadWrite(ref this.coinstake);
            this.CoinstakeSize = stream.ProcessedBytes - prev;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.posBlockHeader.GetHash().ToString();
        }

        // Helpler methods to support legacy format when ProvenBlockHeader inherited from PosBlockHeader

        public uint256 GetHash()
        {
            return this.posBlockHeader.GetHash();
        }

        public uint256 HashPrevBlock { get { return this.posBlockHeader.HashPrevBlock; } }

        public uint Time { get { return this.posBlockHeader.Time; } }

        public Target Bits { get { return this.posBlockHeader.Bits; } }

        protected int version;

        public int Version { get { return this.posBlockHeader.Version; } }

        public uint Nonce { get { return this.posBlockHeader.Nonce; } }

        public uint256 HashMerkleRoot { get { return this.posBlockHeader.HashMerkleRoot; } }
    }
}