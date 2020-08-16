using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.Connection;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Payloads;
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

        private readonly DataStore dataStore;

        private readonly StorageSchemas schemas;

        private readonly StorageSyncronizer sync;

        public StorageBehavior(
            IConnectionManager connectionManager,
            IInitialBlockDownloadState initialBlockDownloadState,
            ILoggerFactory loggerFactory,
            IDataStore dataStore,
            StorageSchemas schemas,
            StorageSyncronizer sync,
            Network network)
        {
            this.dataStore = (DataStore)dataStore; // TODO: When the interface is fixed, we don't need to reference the type directly.
            this.connectionManager = connectionManager;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.loggerFactory = loggerFactory;
            this.schemas = schemas;
            this.sync = sync;
            this.network = network;
            this.lockObject = new object();
        }

        /// <inheritdoc />
        protected override void AttachCore()
        {
            this.AttachedPeer.MessageReceived.Register(this.OnMessageReceivedAsync);
            this.AttachedPeer.StateChanged.Register(this.OnStateChangedAsync);

            // IEnumerable<string> signatures = this.dataStore.GetSignatures("identity", 10, 1);
        }

        /// <inheritdoc />
        protected override void DetachCore()
        {
            this.AttachedPeer.MessageReceived.Unregister(this.OnMessageReceivedAsync);
        }

        private async Task OnStateChangedAsync(INetworkPeer peer, NetworkPeerState oldState)
        {
            if (peer.State == NetworkPeerState.HandShaked)
            {
                _ = Task.Run(async delegate
                  {
                      // Wait 15 seconds before starting the handshake.
                      await Task.Delay(TimeSpan.FromSeconds(15));
                      await SendFeatureHandshake().ConfigureAwait(false);
                  });
            }
        }

        /// <inheritdoc />
        public override object Clone()
        {
            return new StorageBehavior(this.connectionManager, this.initialBlockDownloadState, this.loggerFactory, this.dataStore, this.schemas, this.sync, this.network);
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

        private async Task RequestDocumentsAsync(string collection, string[] signatures)
        {
            INetworkPeer peer = this.AttachedPeer;

            // Send a request to the peer to retrieve the documents of these signatures in the specified collection.
            StoragePayload payload = new StoragePayload
            {
                Version = 1,
                Collections = ConvertASCII(new string[1] { collection }),
                Action = StoragePayloadAction.SendDocuments,
                Signatures = ConvertText(signatures)
            };

            await this.SendStorageQueryAsync(peer, payload).ConfigureAwait(false);
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

            // Mark this peer that it supports storage messages. This is used for outside of behavior (the feature/etc.) to know which peers to forward messages too.
            this.supported = true;

            this.logger.LogInformation("RECEIVED StoragePayload MESSAGE!: {Action}", message.Action);

            // The peer asked to receive signatures.
            if (message.Action == StoragePayloadAction.SendSignatures)
            {
                foreach (VarString collection in message.Collections)
                {
                    var name = Encoders.ASCII.EncodeData(collection.GetString(true));

                    if (name == "identity")
                    {
                        int size = 3; // TODO: Tune the page size.
                        int page = 1;

                        IEnumerable<string> signatures;

                        while ((signatures = this.dataStore.GetSignatures(name, size, page)).Any())
                        {
                            if (!peer.IsConnected)
                            {
                                break;
                            }

                            await this.SendSignaturesAsync(peer, name, signatures);

                            page++;
                        }
                    }
                }
            }
            else if (message.Action == StoragePayloadAction.SendCollections) // Peer asked for collections, send all those documents chunked.
            {
                throw new Exception("Action \"SendCollections\" is currently unsupported.");
                //var name = Encoders.ASCII.EncodeData(collection.GetString(true));

                //// Reply with the documents requested.
                //await this.SendDocumentsAsync(collection, ConvertASCII(message.Signatures));
            }
            else if (message.Action == StoragePayloadAction.SendDocuments) // Peer asked for documents
            {
                if (message.Collections.Length != 1)
                {
                    throw new Exception("The \"SendDocuments\" action require 1 and only 1 collection.");
                }

                var name = Encoders.ASCII.EncodeData(message.Collections[0].GetString(true));

                // Reply with the documents requested.
                await this.SendDocumentsAsync(name, ConvertText(message.Signatures));
            }
        }

        private async Task ProcessStorageInvPayloadAsync(INetworkPeer peer, StorageInvPayload message)
        {
            Guard.NotNull(peer, nameof(peer));

            if (peer != this.AttachedPeer)
            {
                this.logger.LogDebug("Attached peer '{0}' does not match the originating peer '{1}'.", this.AttachedPeer?.RemoteSocketEndpoint, peer.RemoteSocketEndpoint);
                this.logger.LogTrace("(-)[PEER_MISMATCH]");
                return;
            }

            // Mark this peer that it supports storage messages. This is used for outside of behavior (the feature/etc.) to know which peers to forward messages too.
            this.supported = true;

            // Process the receiving of data that we made a request for.
            string collection = Encoders.ASCII.EncodeData(message.Collection.GetString(true));

            this.logger.LogInformation("RECEIVED StorageInvPayload collection name!: {Collection}", collection);

            if (message.Action == StoragePayloadAction.SendSignatures) // We just received a list of signatures, process them.
            {
                IEnumerable<string> items = ConvertText(message.Items);

                foreach (string item in items)
                {
                    this.logger.LogInformation("Signature: {Signature}", item);

                    // Check if we have the signature.
                    // SQL = "SELECT COUNT($.signature) FROM identity WHERE signature = 'IEot8JoTZ+D6ERh6YIKKb3jT1woFJgJ5cgmfd0t5D4W5OlQ8TKDZ+IL9rjjIapx6VzIBOhYWlzVnfynWf5m577M=';";
                    var exists = this.dataStore.ExistsBySignature(item, collection);

                    // If we don't have the document, request it immediately from the node.
                    if (!exists)
                    {
                        this.logger.LogInformation("Please send me this document: {Signature}", item);

                        // TODO: Consider buffering up a list of documents we want, chunks of e.g. 5 or 50, so we reduce chatter.
                        // Until then, we'll simply request every single straight away.
                        await this.RequestDocumentsAsync(collection, new string[1] { item });
                    }
                }
            }
            else if (message.Action == StoragePayloadAction.SendCollections) // We just received a list of documents, process them.
            {
                this.logger.LogDebug($"Received {message.Items.Length} items from peer '{peer.RemoteSocketEndpoint}' for collection {collection}.");

                // TODO: Use generics to map collection string name in a dictionary with the type of the document.
                if (collection == "identity")
                {
                    IdentityDocument[] identities = ConvertAndValidateIdentity(message.Items);

                    foreach (IdentityDocument identity in identities)
                    {
                        // Only persist identities that we support version for.
                        if (!this.schemas.SupportedIdentityVersion(identity.Version))
                        {
                            continue;
                        }

                        IdentityDocument existingIdentity = this.dataStore.GetDocumentById<IdentityDocument>("identity", identity.Content.Identifier);

                        // If the supplied identity is older, don't update,
                        if (existingIdentity != null && existingIdentity.Content.Height > identity.Content.Height)
                        {
                            continue;
                            //var payload = new StorageInvPayload();
                            //payload.Collection = new VarString(Encoders.ASCII.DecodeData("identity"));
                            //payload.Items = ConvertIdentity(new IdentityDocument[1] { existingIdentity });
                            //await peer.SendMessageAsync(payload).ConfigureAwait(false);

                            //return;
                        }

                        // Appears that ID is not sent, even if it was, we should always take it from Content anyway to ensure nobody sends
                        // us data that doesn't belong.
                        identity.Id = "identity/" + identity.Content.Identifier;

                        this.dataStore.SetIdentity(identity);
                    }
                }
            }

            //// Process the receiving of data that we made a request for.
            //string collection = Encoders.ASCII.EncodeData(message.Collection.GetString(true));

            //this.logger.LogDebug($"Received {message.Items.Length} items from peer '{0}' for collection {collection}.", peer.RemoteSocketEndpoint);

            //IdentityDocument[] identities = ConvertIdentity(message.Items);

            //foreach (IdentityDocument identity in identities)
            //{
            //    IdentityDocument existingIdentity = this.dataStore.GetIdentity(identity.Id);

            //    // If the supplied identity is older, don't update, but we will send our copy to the peer.
            //    if (existingIdentity != null && existingIdentity.Version > identity.Version)
            //    {
            //        var payload = new StorageInvPayload();
            //        payload.Collection = new VarString(Encoders.ASCII.DecodeData("identity"));
            //        payload.Items = ConvertIdentity(new IdentityDocument[1] { existingIdentity });
            //        await peer.SendMessageAsync(payload).ConfigureAwait(false);

            //        return;
            //    }

            //    // Only persist identities that we support version for.
            //    if (!this.schemas.SupportedIdentityVersion(identity.Version))
            //    {
            //        continue;
            //    }

            //    this.dataStore.SetIdentity(identity);
            //}
        }

        public async Task SendDocumentsAsync(string collection, IEnumerable<string> signatures)
        {
            INetworkPeer peer = this.AttachedPeer;

            if (peer == null)
            {
                this.logger.LogTrace("(-)[NO_PEER]");
                return;
            }

            if (signatures.Count() > 10)
            {
                throw new Exception("Maximum 10 documents is allowed to be requested at a time.");
            }

            IEnumerable<string> documents = this.dataStore.GetDocuments(collection, signatures);

            var payload = new StorageInvPayload();
            payload.Action = StoragePayloadAction.SendCollections;
            payload.Collection = new VarString(Encoders.ASCII.DecodeData(collection));
            payload.Items = ConvertText(documents); // Converts JSON strings to payload strings.

            await peer.SendMessageAsync(payload).ConfigureAwait(false);
        }

        public async Task SendJsonDocumentsAsync(string collection, IEnumerable<string> documents)
        {
            INetworkPeer peer = this.AttachedPeer;

            if (peer == null)
            {
                this.logger.LogTrace("(-)[NO_PEER]");
                return;
            }

            var payload = new StorageInvPayload();
            payload.Action = StoragePayloadAction.SendCollections;
            payload.Collection = new VarString(Encoders.ASCII.DecodeData(collection));
            payload.Items = ConvertText(documents);

            await peer.SendMessageAsync(payload).ConfigureAwait(false);

            //var queue = new Queue<string>(signatures);

            //while (queue.Count > 0)
            //{
            //    // Send 2 and 2 documents, just for prototype. Increase this later.
            //    IdentityDocument[] items = queue.TakeAndRemove(2).ToArray();

            //    if (!peer.IsConnected)
            //    {
            //        this.logger.LogDebug("Sending items to peer '{0}'.", peer.RemoteSocketEndpoint);

            //        var payload = new StorageInvPayload();
            //        payload.Collection = new VarString(Encoders.ASCII.DecodeData("identity"));
            //        payload.Items = ConvertIdentity(items);

            //        await peer.SendMessageAsync(payload).ConfigureAwait(false);
            //    }
            //}
        }

        private async Task SendSignaturesAsync(INetworkPeer peer, string collection, IEnumerable<string> signatures)
        {
            var payload = new StorageInvPayload();
            payload.Action = StoragePayloadAction.SendSignatures;
            payload.Collection = new VarString(Encoders.ASCII.DecodeData(collection));
            payload.Items = ConvertText(signatures);

            await peer.SendMessageAsync(payload).ConfigureAwait(false);

            //var queue = new Queue<string>(signatures);

            //while (queue.Count > 0)
            //{
            //    // Send 2 and 2 documents, just for prototype. Increase this later.
            //    IdentityDocument[] items = queue.TakeAndRemove(2).ToArray();

            //    if (!peer.IsConnected)
            //    {
            //        this.logger.LogDebug("Sending items to peer '{0}'.", peer.RemoteSocketEndpoint);

            //        var payload = new StorageInvPayload();
            //        payload.Collection = new VarString(Encoders.ASCII.DecodeData("identity"));
            //        payload.Items = ConvertIdentity(items);

            //        await peer.SendMessageAsync(payload).ConfigureAwait(false);
            //    }
            //}
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

        //private bool hasSentHandshake = false;

        private bool supported = false;

        //private DateTime sentHandshakeTime;

        ///// <summary>
        ///// Check if the peer has timed out the verification for supported feature.
        ///// </summary>
        ///// <returns></returns>
        //public bool HasTimedout()
        //{
        //    // If we have not yet sent handshake, we have not timed out yet.
        //    if (!this.hasSentHandshake)
        //    {
        //        return false;
        //    }

        //    TimeSpan span = (DateTime.UtcNow - this.sentHandshakeTime);

        //    return span.TotalSeconds > 30;
        //}

        /// <summary>
        /// Indicates that the peer has been verified to support the feature.
        /// </summary>
        /// <returns></returns>
        public bool Supported()
        {
            return this.supported;
        }

        public async Task SendFeatureHandshake()
        {
            //if (this.hasSentHandshake)
            //{
            //    return;
            //}

            INetworkPeer peer = this.AttachedPeer;

            if (peer == null)
            {
                this.logger.LogTrace("(-)[NO_PEER]");
                return;
            }

            this.logger.LogInformation("Sending storage feature check to peer '{0}'.", peer.RemoteSocketEndpoint);

            try
            {
                var collections = new List<string>();

                collections.Add("identity"); // Identity storage.
                collections.Add("data"); // Generic data storage.
                collections.Add("hub"); // Collection of hub metadata.

                VarString[] list = ConvertASCII(collections);

                // Send a message to the peer what kind of collections we support.
                // If we don't get any response, we'll consider this peer unsupported of storage.
                StoragePayload payload = new StoragePayload
                {
                    Version = 1,
                    Collections = list,
                    Action = StoragePayloadAction.SendSignatures
                };

                // this.sentHandshakeTime = DateTime.UtcNow;

                await this.SendStorageQueryAsync(peer, payload).ConfigureAwait(false);

                // this.hasSentHandshake = true;
            }
            catch (OperationCanceledException ex)
            {
                this.logger.LogTrace("(-)[CANCELED_EXCEPTION]");
                return;
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

        private VarString[] ConvertASCII(IEnumerable<string> list)
        {
            var strings = new List<VarString>();

            foreach (string item in list)
            {
                strings.Add(new VarString(Encoders.ASCII.DecodeData(item)));
            }

            return strings.ToArray();
        }

        private IEnumerable<string> ConvertASCII(VarString[] list)
        {
            var strings = new List<string>();

            foreach (VarString item in list)
            {
                var text = Encoders.ASCII.EncodeData(item.GetString(true));
                strings.Add(text);
            }

            return strings.ToArray();
        }

        private VarString[] ConvertText(IEnumerable<string> list)
        {
            var strings = new List<VarString>();

            foreach (string item in list)
            {
                strings.Add(new VarString(Encoding.UTF8.GetBytes(item)));
            }

            return strings.ToArray();
        }

        private IEnumerable<string> ConvertText(VarString[] list)
        {
            var strings = new List<string>();

            foreach (VarString item in list)
            {
                var text = Encoding.UTF8.GetString(item.GetString(true));
                strings.Add(text);
            }

            return strings.ToArray();
        }

        private VarString[] ConvertIdentity(IdentityDocument[] list)
        {
            var strings = new List<VarString>();

            foreach (IdentityDocument item in list)
            {
                string json = JsonConvert.SerializeObject(item, JsonSettings.Storage);
                strings.Add(new VarString(Encoders.ASCII.DecodeData(json)));
            }

            return strings.ToArray();
        }

        private IdentityDocument[] ConvertAndValidateIdentity(VarString[] list)
        {
            var identities = new List<IdentityDocument>();

            foreach (VarString item in list)
            {
                string jsonText = Encoding.UTF8.GetString(item.GetString(true));
                // string jsonText = Encoders.ASCII.EncodeData(item.GetString(.GetString(true));

                IdentityDocument identity = JsonConvert.DeserializeObject<IdentityDocument>(jsonText);

                // TODO: Validate signature of the document we received over p2p channel.

                // Make sure that we read the .Id from the signed .Content, so it can't be manipulated.
                identity.Id = "identity/" + identity.Content.Identifier;

                identities.Add(identity);
            }

            return identities.ToArray();
        }
    }
}
