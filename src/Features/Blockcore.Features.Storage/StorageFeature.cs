using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration.Logging;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.EventBus;
using Blockcore.EventBus.CoreEvents.Peer;
using Blockcore.Features.Storage.Models;
using Blockcore.Features.Storage.Payloads;
using Blockcore.Features.Storage.Persistence;
using Blockcore.Interfaces;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Signals;
using Blockcore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.Storage
{
    public class StorageFeature : FullNodeFeature
    {
        private readonly ILogger logger;

        /// <summary>The async loop we need to wait upon before we can shut down this manager.</summary>
        private IAsyncLoop asyncLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncProvider asyncProvider;

        /// <summary>
        /// Connection manager injected dependency.
        /// </summary>
        private readonly IConnectionManager connection;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly INodeLifetime nodeLifetime;

        private readonly PayloadProvider payloadProvider;

        private readonly StorageBehavior storageBehavior;

        private readonly IInitialBlockDownloadState ibd;

        private readonly ISignals signals;

        private readonly IConsensusManager consensusManager;

        private SubscriptionToken peerConnectedSubscription;

        private SubscriptionToken peerDisconnectedSubscription;

        public StorageFeature(
            IConnectionManager connection,
            IConsensusManager consensusManager,
            INodeLifetime nodeLifetime,
            IInitialBlockDownloadState ibd,
            ISignals signals,
            IAsyncProvider asyncProvider,
            ILoggerFactory loggerFactory,
            PayloadProvider payloadProvider,
            StorageBehavior storageBehavior)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);

            this.consensusManager = consensusManager;
            this.connection = connection;
            this.nodeLifetime = nodeLifetime;
            this.ibd = ibd;
            this.signals = signals;
            this.payloadProvider = payloadProvider;
            this.asyncProvider = asyncProvider;
            this.storageBehavior = storageBehavior;

            this.peersToVerify = ArrayList.Synchronized(new ArrayList());
            this.verifiedPeers = ArrayList.Synchronized(new ArrayList());
        }

        private readonly ArrayList peersToVerify;

        private readonly ArrayList verifiedPeers;

        private void OnPeerConnected(PeerConnected peerEvent)
        {
            IReadOnlyNetworkPeerCollection peers = this.connection.ConnectedPeers;
            this.peersToVerify.Add(peerEvent.PeerEndPoint);

            INetworkPeer peer = peers.FindByEndpoint(peerEvent.PeerEndPoint);
            this.logger.LogInformation("Peer {Peer} connected and we will investigate if they support storage feature.", peerEvent.PeerEndPoint.Address.ToString());
        }

        private void OnPeerDisconnected(PeerDisconnected peerEvent)
        {
            // Can we simply remove the entry based on the instance itself, or do we need to query based on IP/port?
            this.peersToVerify.Remove(peerEvent.PeerEndPoint);

            this.logger.LogInformation("Peer {Peer} disconnected and removed from verification list.", peerEvent.PeerEndPoint.Address.ToString());
        }

        public override void Dispose()
        {
            this.signals.Unsubscribe(this.peerConnectedSubscription);
            this.signals.Unsubscribe(this.peerDisconnectedSubscription);
        }

        public async Task AnnounceDocument(string collection, string document)
        {
            IReadOnlyNetworkPeerCollection peers = this.connection.ConnectedPeers;

            // Announce the blocks on each nodes behavior which supports the storage behavior.
            IEnumerable<StorageBehavior> behaviors = peers.Select(x => x.Behavior<StorageBehavior>())
                                                          .Where(x => x != null && x.Supported())
                                                          .ToList();

            foreach (StorageBehavior behavior in behaviors)
            {
                await behavior.SendJsonDocumentsAsync(collection, new string[1] { document });
            }
        }

        public override Task InitializeAsync()
        {
            this.peerConnectedSubscription = this.signals.Subscribe<PeerConnected>(this.OnPeerConnected);
            this.peerConnectedSubscription = this.signals.Subscribe<PeerDisconnected>(this.OnPeerDisconnected);

            // Register the behavior.
            this.connection.Parameters.TemplateBehaviors.Add(this.storageBehavior);

            // Register the payload types.
            this.payloadProvider.AddPayload(typeof(StoragePayload));
            this.payloadProvider.AddPayload(typeof(StorageInvPayload));

            // Make a worker that will filter connected nodes that has announced our custom payload behavior.
            this.asyncLoop = this.asyncProvider.CreateAndRunAsyncLoop("Storage.SyncWorker", async token =>
            {
                IReadOnlyNetworkPeerCollection peers = this.connection.ConnectedPeers;

                if (!peers.Any())
                {
                    return;
                }

                if (this.ibd.IsInitialBlockDownload())
                {
                    this.logger.LogTrace("Storage sync will continue after IBD.");
                    return;
                }

                // Go through and verify peers, moving them out of the verify list.
                //for (int i = this.peersToVerify.Count - 1; i >= 0; i--)
                //{
                //    IPEndPoint endpoint = (IPEndPoint)this.peersToVerify[i];

                //    StorageBehavior peerBehavior = peers.Where(p => p.PeerEndPoint == endpoint)
                //                    .Select(p => p.Behavior<StorageBehavior>())
                //                    .Where(p => p != null)
                //                    .SingleOrDefault();

                //    if (peerBehavior != null)
                //    {
                //        // Check if we have received response on handshake, then remove for verify list.
                //        if (peerBehavior.Supported() || peerBehavior.HasTimedout())
                //        {
                //            this.peersToVerify.RemoveAt(i);
                //        }
                //        else
                //        {
                //            await peerBehavior.SendFeatureHandshake().ConfigureAwait(false);
                //        }
                //    }
                //}

                // Announce the blocks on each nodes behavior which supports relaying.
                //IEnumerable<StorageBehavior> behaviors = peers.Where(x => x.PeerVersion?.Relay ?? false)
                //                                              .Select(x => x.Behavior<StorageBehavior>())
                //                                              .Where(x => x != null)
                //                                              .ToList();

                //foreach (StorageBehavior behavior in behaviors)
                //{
                //    await behavior.SendTrickleAsync().ConfigureAwait(false);
                //}
            },
                this.nodeLifetime.ApplicationStopping,
                repeatEvery: TimeSpan.FromSeconds(30), // Run full sync every 15 minutes, this is just a prototype so we want to keep testing the logic.
                startAfter: TimeSpans.TenSeconds); // TODO: Start after 1 minute, loop every 30 seconds.

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderStorageExtension
    {
        public static IFullNodeBuilder UseStorage(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<StorageFeature>("storage");

            StorageSchemas schemas = new StorageSchemas
            {
                IdentityMaxVersion = 3,
                IdentityMinVersion = 3,
                HubMaxVersion = 2,
                HubMinVersion = 2,
                DataMaxVersion = 2,
                DataMinVersion = 2
            };

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                   .AddFeature<StorageFeature>()
                   .FeatureServices(services =>
                   {
                       services.AddSingleton<IDataStore, DataStore>();
                       services.AddSingleton<StorageBehavior>();
                       services.AddSingleton<StorageSyncronizer>();
                       services.AddSingleton(schemas);
                   });
            });

            return fullNodeBuilder;
        }
    }
}
