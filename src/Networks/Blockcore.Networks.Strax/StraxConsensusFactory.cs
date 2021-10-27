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
            return new PosBlockHeader();
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