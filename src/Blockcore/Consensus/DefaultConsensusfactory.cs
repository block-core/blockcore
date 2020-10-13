using System;
using System.Reflection;
using Blockcore.Consensus.BlockInfo;
using NBitcoin.Protocol;

namespace Blockcore.Consensus
{
    /// <summary>
    /// A default object factory to create instances that are not part of the types
    /// that are created by the <see cref="ConsensusFactory"/> like block, block header or transaction.
    /// </summary>
    public sealed class DefaultConsensusFactory : ConsensusFactory
    {
        public DefaultConsensusFactory()
        {
            // Default values that should be fine for cases where the
            // ConsensusFactory is not available from the Network class.
            this.Protocol = new ConsensusProtocol()
            {
                ProtocolVersion = ProtocolVersion.SENDHEADERS_VERSION,
                MinProtocolVersion = ProtocolVersion.SENDHEADERS_VERSION,
            };
        }

        /// <inheritdoc/>
        public override T TryCreateNew<T>()
        {
            if (this.IsBlock<T>() || this.IsBlockHeader<T>() || this.IsTransaction<T>())
                throw new Exception(string.Format("{0} cannot be created by this consensus factory, please use the appropriate one.", typeof(T).Name));

            // Manually add a POS type Proven header
            if (typeof(ProvenBlockHeader).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo()))
                throw new Exception(string.Format("{0} cannot be created by this consensus factory, please use the appropriate one.", typeof(T).Name));

            return default(T);
        }
    }
}