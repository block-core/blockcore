using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Blockcore.P2P.Peer;

namespace Blockcore.Features.Storage
{
    public class StorageSyncronizer
    {
        /// <summary>
        /// TODO: Not implemented yet, the idea is to use it to limit maximum concurrent nodes running sync.
        /// </summary>
        public StorageSyncronizer()
        {
            Queue = new ConcurrentQueue<INetworkPeer>();
        }

        /// <summary>
        /// Queue of peers to sync with. Used to control the maximum amount of current syncing nodes.
        /// </summary>
        public ConcurrentQueue<INetworkPeer> Queue { get; private set; }
    }
}
