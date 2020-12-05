using System.Reflection;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Consensus
{
    /// <summary>
    /// A factory to create protocol types.
    /// </summary>
    public class ConsensusFactory
    {
        /// <summary>
        /// The <see cref="BlockHeader"/> type.
        /// </summary>
        private readonly TypeInfo blockHeaderType = typeof(BlockHeader).GetTypeInfo();

        /// <summary>
        /// The <see cref="Block"/> type.
        /// </summary>
        private readonly TypeInfo blockType = typeof(Block).GetTypeInfo();

        /// <summary>
        /// The <see cref="Transaction"/> type.
        /// </summary>
        private readonly TypeInfo transactionType = typeof(Transaction).GetTypeInfo();

        public ConsensusProtocol Protocol { get; set; }

        public ConsensusFactory()
        {
            this.Protocol = new ConsensusProtocol();
        }

        /// <summary>
        /// Check if the generic type is assignable from <see cref="BlockHeader"/>.
        /// </summary>
        /// <typeparam name="T">The type to check if it is IsAssignable from <see cref="BlockHeader"/>.</typeparam>
        /// <returns><c>true</c> if it is assignable.</returns>
        protected bool IsBlockHeader<T>()
        {
            return this.blockHeaderType.IsAssignableFrom(typeof(T).GetTypeInfo());
        }

        /// <summary>
        /// Check if the generic type is assignable from <see cref="Block"/>.
        /// </summary>
        /// <typeparam name="T">The type to check if it is IsAssignable from <see cref="Block"/>.</typeparam>
        /// <returns><c>true</c> if it is assignable.</returns>
        protected bool IsBlock<T>()
        {
            return this.blockType.IsAssignableFrom(typeof(T).GetTypeInfo());
        }

        /// <summary>
        /// Check if the generic type is assignable from <see cref="Transaction"/>.
        /// </summary>
        /// <typeparam name="T">The type to check if it is IsAssignable from <see cref="Transaction"/>.</typeparam>
        /// <returns><c>true</c> if it is assignable.</returns>
        protected bool IsTransaction<T>()
        {
            return this.transactionType.IsAssignableFrom(typeof(T).GetTypeInfo());
        }

        /// <summary>
        /// A method that will try to resolve a type and determine weather its part of the factory types.
        /// </summary>
        /// <typeparam name="T">The generic type to resolve.</typeparam>
        /// <param name="result">If the type is known it will be initialized.</param>
        /// <returns><c>true</c> if it is known.</returns>
        public virtual T TryCreateNew<T>() where T : IBitcoinSerializable
        {
            object result = null;

            if (this.IsBlock<T>())
                result = (T)(object)this.CreateBlock();
            else if (this.IsBlockHeader<T>())
                result = (T)(object)this.CreateBlockHeader();
            else if (this.IsTransaction<T>())
                result = (T)(object)this.CreateTransaction();

            return (T)result;
        }

        /// <summary>
        /// Create a <see cref="Block"/> instance.
        /// </summary>
        public virtual Block CreateBlock()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new Block(this.CreateBlockHeader());
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Create a <see cref="BlockHeader"/> instance.
        /// </summary>
        public virtual BlockHeader CreateBlockHeader()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new BlockHeader();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Create a <see cref="Transaction"/> instance.
        /// </summary>
        public virtual Transaction CreateTransaction()
        {
            return new Transaction();
        }

        /// <summary>
        /// Create a <see cref="Transaction"/> instance from a hex string representation.
        /// </summary>
        public virtual Transaction CreateTransaction(string hex)
        {
            var transaction = new Transaction();
            transaction.FromBytes(Encoders.Hex.DecodeData(hex));
            return transaction;
        }

        /// <summary>
        /// Create a <see cref="Transaction"/> instance from a byte array representation.
        /// </summary>
        public virtual Transaction CreateTransaction(byte[] bytes)
        {
            var transaction = new Transaction();
            transaction.FromBytes(bytes);
            return transaction;
        }
    }

    public class ConsensusProtocol
    {
        public uint ProtocolVersion { get; set; } = NBitcoin.Protocol.ProtocolVersion.FEEFILTER_VERSION;

        public uint MinProtocolVersion { get; set; } = NBitcoin.Protocol.ProtocolVersion.SENDHEADERS_VERSION;
    }
}