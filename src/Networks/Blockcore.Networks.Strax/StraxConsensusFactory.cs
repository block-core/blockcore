using System.Reflection;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;
using NBitcoin.DataEncoders;

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

    public class StraxConsensusFactory : ConsensusFactory
    {
        /// <summary>
        /// The <see cref="ProvenBlockHeader"/> type.
        /// </summary>
        private readonly TypeInfo provenBlockHeaderType = typeof(ProvenBlockHeader).GetTypeInfo();

        public StraxConsensusFactory()
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