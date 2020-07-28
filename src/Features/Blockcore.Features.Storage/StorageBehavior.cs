using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Connection;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Persistence;
using Blockcore.Interfaces;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol;
using Blockcore.P2P.Protocol.Behaviors;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json;

namespace Blockcore.Features.Storage
{
    /// <summary>
    /// Peer behavior for memory pool.
    /// Provides message handling of notifications from attached peer.
    /// </summary>
    public class StorageBehavior : NetworkPeerBehavior
    {
        /// <summary>Connection manager for managing peer connections.</summary>
        private readonly IConnectionManager connectionManager;

        /// <summary>Provider of IBD state.</summary>
        private readonly IInitialBlockDownloadState initialBlockDownloadState;

        /// <summary>Instance logger for the memory pool behavior component.</summary>
        private readonly ILogger logger;

        /// <summary>Factory used to create the logger for this component.</summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>The network that this component is running on.</summary>
        private readonly Network network;

        /// <summary>
        /// Locking object for memory pool behaviour.
        /// </summary>
        private readonly object lockObject;

        /// <summary>
        /// The min fee the peer asks to relay transactions.
        /// </summary>
        public Money MinFeeFilter { get; set; }

        private readonly DataStore dataStore;

        public StorageBehavior(
            IConnectionManager connectionManager,
            IInitialBlockDownloadState initialBlockDownloadState,
            ILoggerFactory loggerFactory,
            IDataStore dataStore,
            Network network)
        {
            this.dataStore = (DataStore)dataStore; // TODO: When the interface is fixed, we don't need to reference the type directly.
            this.connectionManager = connectionManager;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.loggerFactory = loggerFactory;
            this.network = network;
            this.lockObject = new object();
        }

        /// <inheritdoc />
        protected override void AttachCore()
        {
            this.AttachedPeer.MessageReceived.Register(this.OnMessageReceivedAsync);
        }

        /// <inheritdoc />
        protected override void DetachCore()
        {
            this.AttachedPeer.MessageReceived.Unregister(this.OnMessageReceivedAsync);
        }

        /// <inheritdoc />
        public override object Clone()
        {
            return new StorageBehavior(this.connectionManager, this.initialBlockDownloadState, this.loggerFactory, this.dataStore, this.network);
        }

        private async Task OnMessageReceivedAsync(INetworkPeer peer, IncomingMessage message)
        {
            try
            {
                await this.ProcessMessageAsync(peer, message).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogTrace("(-)[CANCELED_EXCEPTION]");
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogError("Exception occurred: {0}", ex.ToString());
                throw;
            }
        }

        private async Task ProcessMessageAsync(INetworkPeer peer, IncomingMessage message)
        {
            switch (message.Message.Payload)
            {
                case StoragePayload storagePayload:
                    await this.ProcessStoragePayloadAsync(peer, storagePayload).ConfigureAwait(false);
                    break;
                case StorageInvPayload storageInvPayload:
                    await this.ProcessStorageInvPayloadAsync(peer, storageInvPayload).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessStoragePayloadAsync(INetworkPeer peer, StoragePayload message)
        {
            Guard.NotNull(peer, nameof(peer));

            if (peer != this.AttachedPeer)
            {
                this.logger.LogDebug("Attached peer '{0}' does not match the originating peer '{1}'.", this.AttachedPeer?.RemoteSocketEndpoint, peer.RemoteSocketEndpoint);
                this.logger.LogTrace("(-)[PEER_MISMATCH]");
                return;
            }

            // Process the query to get data.
            foreach (VarString collection in message.Collections)
            {
                var name = Encoders.ASCII.EncodeData(collection.GetString(true));

                if (name == "identity")
                {
                    // Get all identities.
                    IEnumerable<IdentityDocument> identities = this.dataStore.GetIdentities();

                    // Send the identities in pages to the peer node.
                    await this.SendIdentityAsync(peer, identities);
                }
            }
        }

        private async Task ProcessStorageInvPayloadAsync(INetworkPeer peer, StorageInvPayload message)
        {
            // Process the receiving of data that we made a request for.
            string collection = Encoders.ASCII.EncodeData(message.Collection.GetString(true));

            this.logger.LogDebug($"Received {message.Items.Length} items from peer '{0}' for collection {collection}.", peer.RemoteSocketEndpoint);

            IdentityDocument[] identities = ConvertIdentity(message.Items);

            foreach (IdentityDocument identity in identities)
            {
                // TODO: Temporarily disable persistence of identities.
                // this.dataStore.SetIdentity(identity);
            }
        }

        private async Task SendIdentityAsync(INetworkPeer peer, IEnumerable<IdentityDocument> identities)
        {
            var queue = new Queue<IdentityDocument>(identities);

            while (queue.Count > 0)
            {
                // Send 2 and 2 documents, just for prototype. Increase this later.
                IdentityDocument[] items = queue.TakeAndRemove(2).ToArray();

                if (peer.IsConnected)
                {
                    this.logger.LogDebug("Sending items to peer '{0}'.", peer.RemoteSocketEndpoint);

                    var payload = new StorageInvPayload();
                    payload.Collection = new VarString(Encoders.ASCII.DecodeData("identity"));
                    payload.Items = ConvertIdentity(items);

                    await peer.SendMessageAsync(payload).ConfigureAwait(false);
                }
            }
        }

        private async Task SendStorageQueryAsync(INetworkPeer peer, StoragePayload payload)
        {
            if (peer.IsConnected)
            {
                this.logger.LogDebug("Sending query for storage to peer '{0}'.", peer.RemoteSocketEndpoint);
                await peer.SendMessageAsync(payload).ConfigureAwait(false);
            }
        }

        public async Task SendTrickleAsync()
        {
            INetworkPeer peer = this.AttachedPeer;

            if (peer == null)
            {
                this.logger.LogTrace("(-)[NO_PEER]");
                return;
            }

            this.logger.LogDebug("Sending storage request (full sync) to peer '{0}'.", peer.RemoteSocketEndpoint);

            try
            {
                var collections = new List<string>();

                collections.Add("identity");
                collections.Add("data");

                VarString[] list = ConvertASCII(collections);

                StoragePayload payload = new StoragePayload
                {
                    Collections = list
                };

                await this.SendStorageQueryAsync(peer, payload).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogTrace("(-)[CANCELED_EXCEPTION]");
                return;
            }
        }

        private VarString[] ConvertASCII(List<string> list)
        {
            var strings = new List<VarString>();

            foreach (string item in list)
            {
                strings.Add(new VarString(Encoders.ASCII.DecodeData(item)));
            }

            return strings.ToArray();
        }

        private VarString[] ConvertIdentity(IdentityDocument[] list)
        {
            var strings = new List<VarString>();

            foreach (IdentityDocument item in list)
            {
                string json = JsonConvert.SerializeObject(item);
                strings.Add(new VarString(Encoders.ASCII.DecodeData(json)));
            }

            return strings.ToArray();
        }

        private IdentityDocument[] ConvertIdentity(VarString[] list)
        {
            var identities = new List<IdentityDocument>();

            foreach (VarString item in list)
            {
                string jsonText = Encoders.ASCII.EncodeData(item.GetString(true));
                IdentityDocument identity = JsonConvert.DeserializeObject<IdentityDocument>(jsonText);

                identities.Add(identity);
            }

            return identities.ToArray();
        }
    }
}
