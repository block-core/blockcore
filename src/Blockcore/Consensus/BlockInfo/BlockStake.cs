using System;
using System.IO;
using System.Reflection;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks;
using NBitcoin;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace Blockcore.Consensus.BlockInfo
{
    [Flags]
    public enum BlockFlag //block index flags
    {
        BLOCK_PROOF_OF_STAKE = (1 << 0), // is proof-of-stake block

        BLOCK_STAKE_ENTROPY = (1 << 1), // entropy bit for stake modifier

        BLOCK_STAKE_MODIFIER = (1 << 2), // regenerated stake modifier
    }

    public class BlockStake : IBitcoinSerializable
    {
        public int Mint;

        public OutPoint PrevoutStake;

        public uint StakeTime;

        public ulong StakeModifier; // hash modifier for proof-of-stake

        public uint256 StakeModifierV2;

        private int flags;

        public uint256 HashProof;

        public BlockStake()
        {
        }

        public BlockFlag Flags
        {
            get
            {
                return (BlockFlag)this.flags;
            }

            set
            {
                this.flags = (int)value;
            }
        }

        public static bool IsProofOfStake(Block block)
        {
            return block.Transactions.Count > 1 && block.Transactions[1].IsCoinStake;
        }

        public static bool IsProofOfWork(Block block)
        {
            return !IsProofOfStake(block);
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.flags);
            stream.ReadWrite(ref this.Mint);
            stream.ReadWrite(ref this.StakeModifier);
            stream.ReadWrite(ref this.StakeModifierV2);

            if (this.IsProofOfStake())
            {
                stream.ReadWrite(ref this.PrevoutStake);
                stream.ReadWrite(ref this.StakeTime);
            }

            stream.ReadWrite(ref this.HashProof);
        }

        public bool IsProofOfWork()
        {
            return !((this.Flags & BlockFlag.BLOCK_PROOF_OF_STAKE) > 0);
        }

        public bool IsProofOfStake()
        {
            return (this.Flags & BlockFlag.BLOCK_PROOF_OF_STAKE) > 0;
        }

        public void SetProofOfStake()
        {
            this.Flags |= BlockFlag.BLOCK_PROOF_OF_STAKE;
        }

        public uint GetStakeEntropyBit()
        {
            return (uint)(this.Flags & BlockFlag.BLOCK_STAKE_ENTROPY) >> 1;
        }

        public bool SetStakeEntropyBit(uint nEntropyBit)
        {
            if (nEntropyBit > 1)
                return false;
            this.Flags |= (nEntropyBit != 0 ? BlockFlag.BLOCK_STAKE_ENTROPY : 0);
            return true;
        }

        /// <summary>
        /// Constructs a stake block from a given block.
        /// </summary>
        public static BlockStake Load(Block block)
        {
            var blockStake = new BlockStake
            {
                StakeModifierV2 = uint256.Zero,
                HashProof = uint256.Zero
            };

            if (IsProofOfStake(block))
            {
                blockStake.SetProofOfStake();
                blockStake.StakeTime = block.Header.Time;
                blockStake.PrevoutStake = block.Transactions[1].Inputs[0].PrevOut;
            }

            return blockStake;
        }

        /// <summary>
        /// Constructs a stake block from a set bytes and the given network.
        /// </summary>
        public static BlockStake Load(byte[] bytes, ConsensusFactory consensusFactory)
        {
            var blockStake = new BlockStake();
            blockStake.ReadWrite(bytes, consensusFactory);
            return blockStake;
        }

        /// <summary>
        /// Check PoW and that the blocks connect correctly
        /// </summary>
        /// <param name="network">The network being used</param>
        /// <param name="chainedHeader">Chained block header</param>
        /// <returns>True if PoW is correct</returns>
        public static bool Validate(Network network, ChainedHeader chainedHeader)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            if (chainedHeader.Height != 0 && chainedHeader.Previous == null)
                return false;

            bool heightCorrect = chainedHeader.Height == 0 || chainedHeader.Height == chainedHeader.Previous.Height + 1;
            bool genesisCorrect = chainedHeader.Height != 0 || chainedHeader.HashBlock == network.GetGenesis().GetHash();
            bool hashPrevCorrect = chainedHeader.Height == 0 || chainedHeader.Header.HashPrevBlock == chainedHeader.Previous.HashBlock;
            bool hashCorrect = chainedHeader.HashBlock == chainedHeader.Header.GetHash();

            return heightCorrect && genesisCorrect && hashPrevCorrect && hashCorrect;
        }
    }

    /// <summary>
    ///  Represents a transaction with a time field, such trx are used by POS networks however
    ///  the time field is not really needed anymore for POS consensus so in order to allow a new PoSv4 protocol
    ///  network we make this field encapsolated by an interface, removing the time field will make POS
    ///  transactions have the same serialization format as Bitcoin.
    /// </summary>
    public interface IPosTransactionWithTime
    {
        uint Time { get; set; }
    }

    /// <summary>
    /// A Proof Of Stake transaction.
    /// </summary>
    public class PosTransaction : Transaction, IPosTransactionWithTime
    {
        private uint nTime = Utils.DateTimeToUnixTime(DateTime.UtcNow);

        public uint Time
        {
            get
            {
                return this.nTime;
            }

            set
            {
                this.nTime = value;
            }
        }

        public PosTransaction() : base()
        {
        }

        public PosTransaction(string hex, ConsensusFactory consensusFactory) : this()
        {
            this.FromBytes(Encoders.Hex.DecodeData(hex), consensusFactory);
        }

        public PosTransaction(byte[] bytes) : this()
        {
            this.FromBytes(bytes);
        }

        public override bool IsProtocolTransaction()
        {
            return this.IsCoinStake || this.IsCoinBase;
        }

        public override void ReadWrite(BitcoinStream stream)
        {
            bool witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
                                stream.ProtocolVersion >= ProtocolVersion.WITNESS_VERSION;

            byte flags = 0;
            if (!stream.Serializing)
            {
                stream.ReadWrite(ref this.nVersion);

                // POS time stamp
                stream.ReadWrite(ref this.nTime);

                /* Try to read the vin. In case the dummy is there, this will be read as an empty vector. */
                stream.ReadWrite<TxInList, TxIn>(ref this.vin);

                bool hasNoDummy = (this.nVersion & NoDummyInput) != 0 && this.vin.Count == 0;
                if (witSupported && hasNoDummy) this.nVersion = this.nVersion & ~NoDummyInput;

                if (this.vin.Count == 0 && witSupported && !hasNoDummy)
                {
                    /* We read a dummy or an empty vin. */
                    stream.ReadWrite(ref flags);
                    if (flags != 0)
                    {
                        /* Assume we read a dummy and a flag. */
                        stream.ReadWrite<TxInList, TxIn>(ref this.vin);
                        this.vin.Transaction = this;
                        stream.ReadWrite<TxOutList, TxOut>(ref this.vout);
                        this.vout.Transaction = this;
                    }
                    else
                    {
                        /* Assume read a transaction without output. */
                        this.vout = new TxOutList();
                        this.vout.Transaction = this;
                    }
                }
                else
                {
                    /* We read a non-empty vin. Assume a normal vout follows. */
                    stream.ReadWrite<TxOutList, TxOut>(ref this.vout);
                    this.vout.Transaction = this;
                }

                if (((flags & 1) != 0) && witSupported)
                {
                    /* The witness flag is present, and we support witnesses. */
                    flags ^= 1;
                    var wit = new Witness(this.Inputs);
                    wit.ReadWrite(stream);
                }

                if (flags != 0)
                {
                    /* Unknown flag in the serialization */
                    throw new FormatException("Unknown transaction optional data");
                }
            }
            else
            {
                uint version = (witSupported && (this.vin.Count == 0 && this.vout.Count > 0)) ? this.nVersion | NoDummyInput : this.nVersion;
                stream.ReadWrite(ref version);

                // the POS time stamp
                stream.ReadWrite(ref this.nTime);

                if (witSupported)
                {
                    /* Check whether witnesses need to be serialized. */
                    if (this.HasWitness)
                    {
                        flags |= 1;
                    }
                }

                if (flags != 0)
                {
                    /* Use extended format in case witnesses are to be serialized. */
                    var vinDummy = new TxInList();
                    stream.ReadWrite<TxInList, TxIn>(ref vinDummy);
                    stream.ReadWrite(ref flags);
                }

                stream.ReadWrite<TxInList, TxIn>(ref this.vin);
                this.vin.Transaction = this;
                stream.ReadWrite<TxOutList, TxOut>(ref this.vout);
                this.vout.Transaction = this;
                if ((flags & 1) != 0)
                {
                    var wit = new Witness(this.Inputs);
                    wit.ReadWrite(stream);
                }
            }

            stream.ReadWriteStruct(ref this.nLockTime);
        }
    }

    /// <summary>
    /// The consensus factory for creating POS protocol types.
    /// </summary>
    public class PosConsensusFactory : ConsensusFactory
    {
        /// <summary>
        /// The <see cref="ProvenBlockHeader"/> type.
        /// </summary>
        private readonly TypeInfo provenBlockHeaderType = typeof(ProvenBlockHeader).GetTypeInfo();

        public PosConsensusFactory()
            : base()
        {
        }

        /// <summary>
        /// Check if the generic type is assignable from <see cref="BlockHeader"/>.
        /// </summary>
        /// <typeparam name="T">The type to check if it is IsAssignable from <see cref="BlockHeader"/>.</typeparam>
        /// <returns><c>true</c> if it is assignable.</returns>
        protected bool IsProvenBlockHeader<T>()
        {
            return this.provenBlockHeaderType.IsAssignableFrom(typeof(T).GetTypeInfo());
        }

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
            return new PosBlockHeader();
        }

        public virtual ProvenBlockHeader CreateProvenBlockHeader()
        {
            return new ProvenBlockHeader();
        }

        public virtual ProvenBlockHeader CreateProvenBlockHeader(PosBlock block)
        {
            var provenBlockHeader = new ProvenBlockHeader(block, (PosBlockHeader)this.CreateBlockHeader());

            // Serialize the size.
            provenBlockHeader.ToBytes(this);

            return provenBlockHeader;
        }

        /// <inheritdoc />
        public override Transaction CreateTransaction()
        {
            return new PosTransaction();
        }

        /// <inheritdoc />
        public override Transaction CreateTransaction(string hex)
        {
            return new PosTransaction(hex, this);
        }

        /// <inheritdoc />
        public override Transaction CreateTransaction(byte[] bytes)
        {
            return new PosTransaction(bytes);
        }
    }

    /// <summary>
    /// A POS block header, this will create a work hash based on the X13 hash algos.
    /// </summary>
#pragma warning disable 618

    public class PosBlockHeader : BlockHeader
#pragma warning restore 618
    {
        /// <inheritdoc />
        public override int CurrentVersion => 7;

        /// <inheritdoc />
        public override uint256 GetHash()
        {
            uint256 hash = null;
            uint256[] innerHashes = this.hashes;

            if (innerHashes != null)
                hash = innerHashes[0];

            if (hash != null)
                return hash;

            if (this.version > 6)
            {
                using (var hs = new HashStream())
                {
                    this.ReadWriteHashingStream(new BitcoinStream(hs, true));
                    hash = hs.GetHash();
                }
            }
            else
            {
                hash = this.GetPoWHash();
            }

            innerHashes = this.hashes;
            if (innerHashes != null)
            {
                innerHashes[0] = hash;
            }

            return hash;
        }

        /// <inheritdoc />
        public override uint256 GetPoWHash()
        {
            using (var ms = new MemoryStream())
            {
                this.ReadWriteHashingStream(new BitcoinStream(ms, true));
                return HashX13.Instance.Hash(ms.ToArray());
            }
        }

        public ProvenBlockHeader ProvenBlockHeader { get; set; }
    }

    /// <summary>
    /// A POS block that contains the additional block signature serialization.
    /// </summary>
    public class PosBlock : Block
    {
        /// <summary>
        /// A block signature - signed by one of the coin base txout[N]'s owner.
        /// </summary>
        private BlockSignature blockSignature = new BlockSignature();

        [Obsolete("Should use Block.Load outside of ConsensusFactories")]
        public PosBlock(BlockHeader blockHeader) : base(blockHeader)
        {
        }

        /// <summary>
        /// The block signature type.
        /// </summary>
        public BlockSignature BlockSignature
        {
            get { return this.blockSignature; }
            set { this.blockSignature = value; }
        }

        /// <summary>
        /// The additional serialization of the block POS block.
        /// </summary>
        public override void ReadWrite(BitcoinStream stream)
        {
            // Capture the value in BlockSize as calling base will change it.
            long? blockSize = this.BlockSize;

            base.ReadWrite(stream);
            stream.ReadWrite(ref this.blockSignature);

            if (blockSize == null)
            {
                this.BlockSize = stream.Serializing ? stream.Counter.WrittenBytes : stream.Counter.ReadBytes;
            }
        }

        /// <summary>
        /// Gets the block's coinstake transaction or returns the coinbase transaction if there is no coinstake.
        /// </summary>
        /// <returns>Coinstake transaction or coinbase transaction.</returns>
        /// <remarks>
        /// <para>In PoS blocks, coinstake transaction is the second transaction in the block.</para>
        /// <para>In PoW there isn't a coinstake transaction, return coinbase instead to be able to compute stake modifier for the next eventual PoS block.</para>
        /// </remarks>
        public Transaction GetProtocolTransaction()
        {
            return (this.Transactions.Count > 1 && this.Transactions[1].IsCoinStake) ? this.Transactions[1] : this.Transactions[0];
        }
    }
}