using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.P2P.Protocol.Payloads
{
    public enum StoragePayloadAction
    { 
        SupportedCollections = 0,
        SendCollections = 1,
        SendSignatures = 2
    }


    [Payload("storage")]
    public class StoragePayload : Payload
    {
        private VarString[] collections;

        /// <summary>
        /// Name of collections that a node want to retrieve from other nodes.
        /// </summary>
        public VarString[] Collections { get { return this.collections; } set { this.collections = value; } }

        private ulong action;

        public StoragePayloadAction Action
        {
            get
            {
                return (StoragePayloadAction)this.action;
            }

            set
            {
                this.action = (ulong)value;
            }
        }

        private ushort version = 1;

        public ushort Version { get { return this.version; } set { this.version = value; } }

        public StoragePayload(VarString[] collections)
        {
            this.collections = collections;
        }

        public StoragePayload()
        {

        }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.version);
            stream.ReadWrite(ref this.action);
            stream.ReadWrite(ref this.collections);
        }

        public override string ToString()
        {
            return base.ToString() + " : " + this.Collections;
        }
    }
}