using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using NBitcoin;

namespace Blockcore.Features.PoA
{
    public class PoAConsensusFactory : ConsensusFactory
    {
        /// <inheritdoc />
        public override Block CreateBlock()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return new Block(this.CreateBlockHeader());
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <inheritdoc />
        public override BlockHeader CreateBlockHeader()
        {
            return new PoABlockHeader();
        }

        public virtual IFederationMember DeserializeFederationMember(byte[] serializedBytes)
        {
            var key = new PubKey(serializedBytes);

            IFederationMember federationMember = new FederationMember(key);

            return federationMember;
        }

        public virtual byte[] SerializeFederationMember(IFederationMember federationMember)
        {
            return federationMember.PubKey.ToBytes();
        }
    }
}