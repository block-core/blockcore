using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.P2P.Protocol.Payloads
{
    [Payload("storage")]
    public class StoragePayload : Payload
    {
        private VarString[] collections;

        /// <summary>
        /// Name of collections that a node want to retrieve from other nodes.
        /// </summary>
        public VarString[] Collections { get { return this.collections; } set { this.collections = value; } }

        public StoragePayload(VarString[] collections)
        {
            this.collections = collections;
        }

        public StoragePayload()
        {

        }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.collections);
        }

        public override string ToString()
        {
            return base.ToString() + " : " + this.Collections;
        }
    }
}