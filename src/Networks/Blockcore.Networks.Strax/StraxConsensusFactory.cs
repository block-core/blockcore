using System.Reflection;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.DataEncoders;

namespace Blockcore.Networks.Strax
{
    public class StraxTransaction : Transaction
    {
        public StraxTransaction() : base()
        {
        }

        public StraxTransaction(string hex, ConsensusFactory consensusFactory) : this()
        {
            this.FromBytes(Encoders.Hex.DecodeData(hex), consensusFactory);
        }

        public StraxTransaction(byte[] bytes) : this()
        {
            this.FromBytes(bytes);
        }

        public override bool IsProtocolTransaction()
        {
            return this.IsCoinStake || this.IsCoinBase;
        }
    }

    public class StraxPosBlockHeader : PosBlockHeader
    {
        // Indicates that the header contains additional fields.
        // The first field is a uint "Size" field to indicate the serialized size of additional fields.
        public const int ExtendedHeaderBit = 0x10000000;

        // Determines whether this object should serialize the new fields associated with smart contracts.
        public bool HasSmartContractFields => (this.version & ExtendedHeaderBit) != 0;

        /// <inheritdoc />
        public override int CurrentVersion => 7;

        private ushort extendedHeaderSize => (ushort)(hashStateRootSize + receiptRootSize + this.logsBloom.GetCompressedSize());

        /// <summary>
        /// Root of the state trie after execution of this block. 
        /// </summary>
        private uint256 hashStateRoot;
        public uint256 HashStateRoot { get { return this.hashStateRoot; } set { this.hashStateRoot = value; } }
        private static int hashStateRootSize = 32;

        /// <summary>
        /// Root of the receipt trie after execution of this block.
        /// </summary>
        private uint256 receiptRoot;
        public uint256 ReceiptRoot { get { return this.receiptRoot; } set { this.receiptRoot = value; } }
        private static int receiptRootSize = 32;

        /// <summary>
        /// Bitwise-OR of all the blooms generated from all of the smart contract transactions in the block.
        /// </summary>
        private Bloom logsBloom;
        public Bloom LogsBloom { get { return this.logsBloom; } set { this.logsBloom = value; } }

        public StraxPosBlockHeader()
        {
            this.hashStateRoot = 0;
            this.receiptRoot = 0;
            this.logsBloom = new Bloom();
        }

        #region IBitcoinSerializable Members

        public override void ReadWrite(BitcoinStream stream)
        {
            base.ReadWrite(stream);
            if (this.HasSmartContractFields)
            {
                stream.ReadWrite(ref this.hashStateRoot);
                stream.ReadWrite(ref this.receiptRoot);
                stream.ReadWriteCompressed(ref this.logsBloom);
            }
        }

        #endregion

        /// <summary>Populates stream with items that will be used during hash calculation.</summary>
        protected override void ReadWriteHashingStream(BitcoinStream stream)
        {
            base.ReadWriteHashingStream(stream);
            if (this.HasSmartContractFields)
            {
                stream.ReadWrite(ref this.hashStateRoot);
                stream.ReadWrite(ref this.receiptRoot);
                stream.ReadWriteCompressed(ref this.logsBloom);
            }
        }

        /// <summary>Gets the total header size - including the <see cref="BlockHeader.Size"/> - in bytes.</summary>
        public override long HeaderSize => this.HasSmartContractFields ? Size + this.extendedHeaderSize : Size;
    }


    public class StraxConsensusFactory : PosConsensusFactory
    {
        /// <inheritdoc />
        public override T TryCreateNew<T>()
        {
            if (this.IsProvenBlockHeader<T>())
                return (T)(object)this.CreateProvenBlockHeader();

            return base.TryCreateNew<T>();
        }

        /// <inheritdoc />
        public override Block CreateBlock()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new PosBlock(this.CreateBlockHeader());
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <inheritdoc />
        public override BlockHeader CreateBlockHeader()
        {
            return new StraxPosBlockHeader();
        }

        /// <inheritdoc />
        public override Transaction CreateTransaction()
        {
            return new StraxTransaction();
        }

        /// <inheritdoc />
        public override Transaction CreateTransaction(string hex)
        {
            return new StraxTransaction(hex, this);
        }

        /// <inheritdoc />
        public override Transaction CreateTransaction(byte[] bytes)
        {
            return new StraxTransaction(bytes);
        }
    }
}