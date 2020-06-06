using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Blockcore.AsyncWork;
using Blockcore.Configuration;
using Blockcore.Utilities;
using x42.Features.xServer.Interfaces;
using x42.Features.xServer.Models;
using NBitcoin.Protocol;
using System.Collections.Concurrent;

namespace x42.Features.xServer
{
    public class xServerManager : IxServerManager
    {
        /// <inheritdoc />
        public List<xServerPeer> ConnectedSeeds
        {
            get
            {
                return this.xServerPeerList.GetPeers();
            }
        }

        /// <summary>
        /// The xServer peers list manager.
        /// </summary>
        private readonly xServerPeers xServerPeerList;

        /// <summary>
        /// Defines the name of the xServer peers on disk.
        /// </summary>
        private const string xServerPeersFileName = "xserverpeers.json";

        /// <summary>
        /// Sets the period by which the xServer discovery check occurs (secs).
        /// </summary>
        private const int CheckXServerRate = 600;

        /// <summary>
        /// Protects access to the list of xServer Peers.
        /// </summary>
        private readonly object xServerPeersLock;

        /// <summary>
        /// Defines the data folders of the system.
        /// </summary>
        private readonly DataFolder dataFolders;

        /// <summary>
        /// Provider for creating and managing async loops.
        /// </summary>
        private readonly IAsyncProvider asyncProvider;

        /// <summary>
        /// Will discover and manage xServers.
        /// </summary>
        private IAsyncLoop xServerDiscoveryLoop;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// Defines a node lifetime object.
        /// </summary>
        private readonly INodeLifetime nodeLifetime;

        /// <summary>
        /// The xServer Settings.
        /// </summary>
        readonly xServerSettings xServerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="xServerFeature"/> class with the xServers.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="dataFolders">The data folders of the system.</param>
        /// <param name="asyncProvider">The async loop factory.</param>
        /// <param name="nodeLifetime">The managment of the node process.</param>
        /// <param name="network">The network managment.</param>
        public xServerManager(
            ILoggerFactory loggerFactory,
            DataFolder dataFolders,
            IAsyncProvider asyncProvider,
            INodeLifetime nodeLifetime,
            xServerSettings xServerSettings)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(dataFolders, nameof(dataFolders));
            Guard.NotNull(asyncProvider, nameof(asyncProvider));
            Guard.NotNull(nodeLifetime, nameof(nodeLifetime));
            Guard.NotNull(xServerSettings, nameof(xServerSettings));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.dataFolders = dataFolders;
            this.asyncProvider = asyncProvider;
            this.nodeLifetime = nodeLifetime;
            this.xServerSettings = xServerSettings;

            string path = Path.Combine(this.dataFolders.xServerAppsPath, xServerPeersFileName);
            this.xServerPeerList = new xServerPeers(path);

            this.xServerPeersLock = new object();
        }

        /// <inheritdoc />
        public void Start()
        {
            this.xServerDiscoveryLoop = this.asyncProvider.CreateAndRunAsyncLoop($"{nameof(xServerFeature)}.xServerSeedRefresh", async token =>
            {
                await XServerDiscoveryAsync(this.xServerPeerList).ConfigureAwait(false);
                this.logger.LogInformation($"Saving cached xServer Seeds to {this.xServerPeerList.Path}");
                await this.xServerPeerList.Save().ConfigureAwait(false);
            },
            this.nodeLifetime.ApplicationStopping,
            repeatEvery: TimeSpan.FromSeconds(CheckXServerRate));
        }

        /// <inheritdoc />
        public void Stop()
        {
            this.xServerDiscoveryLoop?.Dispose();
        }

        /// <inheritdoc />
        public RegisterResult RegisterXServer(RegisterRequest registerRequest)
        {
            var result = new RegisterResult();

            var client = new RestClient(GetAddress(registerRequest));
            var registerRestRequest = new RestRequest("/register", Method.POST);
            var request = JsonConvert.SerializeObject(registerRequest);
            registerRestRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
            registerRestRequest.RequestFormat = DataFormat.Json;

            var registerResult = client.Execute<RegisterResult>(registerRestRequest);
            if (registerResult.StatusCode == HttpStatusCode.OK)
            {
                result = registerResult.Data;
            }
            else
            {
                result.ResultMessage = "Failed to access xServer";
                result.Success = false;
            }
            return result;
        }

        private string GetAddress(NetworkXServer networkAddress)
        {
            return $"{(networkAddress.IsSSL ? "https" : "http")}://{networkAddress.PublicAddress}:{networkAddress.Port}";
        }

        private string GetAddress(RegisterRequest registerRequest)
        {
            return $"{(registerRequest.NetworkProtocol == 1 ? "http" : "https")}://{registerRequest.NetworkAddress}:{registerRequest.NetworkPort}";
        }

        private void SyncPeerToPeersList(xServerPeers xServerPeerList, xServerPeer peer, bool seedCheck = false)
        {
            lock (this.xServerPeersLock)
            {
                var peersList = xServerPeerList.GetPeers();
                int peerIndex = peersList.FindIndex(p => p.Name == peer.Name && p.Address == peer.Address);
                if (seedCheck)
                {
                    if (peerIndex == -1)
                    {
                        peersList.Add(peer);
                    }
                }
                else
                {
                    if (peerIndex >= 0)
                    {
                        peersList[peerIndex] = peer;
                    }
                    else
                    {
                        peersList.Add(peer);
                    }
                }
                xServerPeerList.ReplacePeers(peersList);
            }
        }

        private void SyncSeedsToPeersList(xServerPeers xServerPeerList, ConcurrentBag<NetworkXServer> seedList)
        {
            foreach (var networkAddress in seedList)
            {
                var seedPeer = new xServerPeer()
                {
                    Name = "Public Seed",
                    Address = networkAddress.PublicAddress,
                    Port = networkAddress.Port,
                    Priority = -1,
                    Version = "N/A",
                    ResponseTime = 99999999,
                    Tier = (int)TierLevel.Seed
                };
                SyncPeerToPeersList(xServerPeerList, seedPeer, true);
            }
        }

        private async Task XServerDiscoveryAsync(xServerPeers xServerPeerList)
        {
            var seedList = new ConcurrentBag<NetworkXServer>();
            int topResult = 10;
            await this.xServerSettings.RetrieveNodes().ForEachAsync(10, this.nodeLifetime.ApplicationStopping, async (networkAddress, cancellation) =>
            {
                if (this.nodeLifetime.ApplicationStopping.IsCancellationRequested)
                    return;

                string xServerURL = $"{(networkAddress.IsSSL ? "https" : "http")}://{networkAddress.PublicAddress}:{networkAddress.Port}";

                this.logger.LogDebug($"Attempting connection to {xServerURL}.");

                var client = new RestClient(xServerURL);
                var topXServersRequest = new RestRequest("/gettop/", Method.GET);
                topXServersRequest.AddParameter("top", topResult);
                var topXServerResult = await client.ExecuteAsync<TopResult>(topXServersRequest, cancellation).ConfigureAwait(false);
                if (topXServerResult.StatusCode == HttpStatusCode.OK)
                {
                    seedList.Add(networkAddress);
                    if (topXServerResult.Data?.XServers?.Count > 0)
                    {
                        var xServers = topXServerResult.Data.XServers;
                        foreach (var xServer in xServers)
                        {
                            var pingRequest = new RestRequest("/ping/", Method.GET);
                            var pingResponseTime = Stopwatch.StartNew();
                            var pingResult = await client.ExecuteAsync<PingResult>(pingRequest, cancellation).ConfigureAwait(false);
                            pingResponseTime.Stop();
                            if (topXServerResult.StatusCode == HttpStatusCode.OK)
                            {
                                var ping = pingResult.Data;
                                var peer = new xServerPeer()
                                {
                                    Name = xServer.Name,
                                    Address = xServer.Address,
                                    Port = xServer.Port,
                                    Priority = xServer.Priotiry,
                                    Version = ping.Version,
                                    ResponseTime = pingResponseTime.ElapsedMilliseconds,
                                    Tier = xServer.Tier
                                };
                                SyncPeerToPeersList(xServerPeerList, peer);
                            }
                        }
                    }
                }

                SyncSeedsToPeersList(xServerPeerList, seedList);

            }).ConfigureAwait(false);
        }
    }
}
