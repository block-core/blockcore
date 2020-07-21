using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.P2P.Protocol.Payloads
{
    [Payload("storageinv")]
    public class StorageInvPayload : Payload
    {
        private VarString collection;
        private VarString[] items;

        /// <summary>
        /// Name of the collection the items belong to.
        /// </summary>
        public VarString Collection { get { return this.collection; } set { this.collection = value; } }

        /// <summary>
        /// The items that belongs to the specified collection.
        /// </summary>
        public VarString[] Items { get { return this.items; } set { this.items = value; } }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.collection);
            stream.ReadWrite(ref this.items);
        }

        public override string ToString()
        {
            return base.ToString() + " : " + this.Collection;
        }
    }
}