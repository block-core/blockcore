using System.Collections.Generic;
using System.Linq;
using Blockcore.P2P.Protocol.Payloads;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.Features.Storage.Payloads
{
    /// <summary>
    /// /// The StorageInvPayload is the type that responds to queries and returns data.
    /// </summary>
    [Payload("storageinv")]
    public class StorageInvPayload : Payload
    {
        private VarString collection;

        private VarString[] items = new VarString[0];

        private ulong action;

        private ushort version = 1;

        /// <summary>
        /// Name of the collection the items belong to.
        /// </summary>
        public VarString Collection { get { return this.collection; } set { this.collection = value; } }

        /// <summary>
        /// The items that belongs to the specified collection.
        /// </summary>
        public VarString[] Items { get { return this.items; } set { this.items = value; } }

        public ushort Version { get { return this.version; } set { this.version = value; } }

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

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.version);
            stream.ReadWrite(ref this.action);
            stream.ReadWrite(ref this.collection);
            stream.ReadWrite(ref this.items);
        }

        public override string ToString()
        {
            return base.ToString() + " : " + this.Collection;
        }
    }
}