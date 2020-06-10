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
using NBitcoin;
using System.Net.Sockets;

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

        // <summary>The network the node is running on.</summary>
        private readonly Network network;

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
            xServerSettings xServerSettings,
            Network network)
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
            this.network = network;

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
            string xServerURL = Utils.GetServerUrl(registerRequest.NetworkProtocol, registerRequest.NetworkAddress, registerRequest.NetworkPort);
            var client = new RestClient(xServerURL);
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

        /// <inheritdoc />
        public TestResult TestXServerPorts(TestRequest testRequest)
        {
            TestResult testResult = new TestResult()
            {
                Success = true
            };

            if (!ValidateNodeOnline(testRequest))
            {
                testResult.ResultMessage = $"Test failed, node unavailable on port {this.network.DefaultPort}";
                testResult.Success = false;
            }

            string connectedAndSyncedMessage = GetServerOnlineAndSyncedMessage(testRequest);
            if (connectedAndSyncedMessage != string.Empty)
            {
                testResult.ResultMessage = connectedAndSyncedMessage;
                testResult.Success = false;
            }

            return testResult;
        }

        private bool ValidateNodeOnline(TestRequest testRequest)
        {
            bool result = false;
            try
            {
                using var client = new TcpClient(testRequest.NetworkAddress, this.network.DefaultPort);
                result = true;
            }
            catch (SocketException) { }
            return result;
        }

        private string GetServerOnlineAndSyncedMessage(TestRequest testRequest)
        {
            string result = string.Empty;
            try
            {
                string xServerURL = Utils.GetServerUrl(testRequest.NetworkProtocol, testRequest.NetworkAddress, testRequest.NetworkPort);

                this.logger.LogDebug($"Attempting validate connection to {xServerURL}.");

                var client = new RestClient(xServerURL);
                var xServersPingRequest = new RestRequest("/ping", Method.GET);
                var xServerPingResult = client.Execute<PingResult>(xServersPingRequest);
                if (xServerPingResult.StatusCode == HttpStatusCode.OK)
                {
                    long minimumBlockHeight = Convert.ToInt64(xServerPingResult.Data.BestBlockHeight) + 6; // TODO: This 6 is an xServer consensus.
                    if (minimumBlockHeight < testRequest.BlockHeight)
                    {
                        result = $"The xServer is not sync'd to network, it's on block {xServerPingResult.Data.BestBlockHeight} but needs to be on {testRequest.BlockHeight}";
                    }
                }
                else
                {
                    result = $"Test failed, xServer unavailable on port {testRequest.NetworkPort}";
                }
            }
            catch (Exception)
            {
                result = $"Test failed, xServer unavailable on port {testRequest.NetworkPort}";
            }
            return result;
        }

        private void SyncPeerToPeersList(xServerPeers xServerPeerList, xServerPeer peer, bool seedCheck = false)
        {
            lock (this.xServerPeersLock)
            {
                var peersList = xServerPeerList.GetPeers();
                int peerIndex = peersList.FindIndex(p => p.Address == peer.Address);
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
                    NetworkProtocol = networkAddress.NetworkProtocol,
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

                string xServerURL = Utils.GetServerUrl(networkAddress.NetworkProtocol, networkAddress.PublicAddress, networkAddress.Port);

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
                                    NetworkProtocol = xServer.NetworkProtocol,
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
