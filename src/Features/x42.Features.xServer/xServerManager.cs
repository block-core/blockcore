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
using System.Linq;
using RestSharp.Serializers.NewtonsoftJson;

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
        /// Sets the period by which the xServer refresh check occurs (secs).
        /// </summary>
        private const int RefreshXServerRate = 60;

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

        /// <summary>
        /// Will discover and manage xServers.
        /// </summary>
        private IAsyncLoop xServerRefreshLoop;

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
            this.xServerDiscoveryLoop = this.asyncProvider.CreateAndRunAsyncLoop($"{nameof(xServerFeature)}.xServerSeedDiscover", async token =>
            {
                await XServerDiscoveryAsync(this.xServerPeerList).ConfigureAwait(false);
                this.logger.LogInformation($"Saving new xServer Seeds to {this.xServerPeerList.Path}");
                await this.xServerPeerList.Save().ConfigureAwait(false);
            },
            this.nodeLifetime.ApplicationStopping,
            repeatEvery: TimeSpan.FromSeconds(CheckXServerRate),
            startAfter: TimeSpans.Second);

            this.xServerRefreshLoop = this.asyncProvider.CreateAndRunAsyncLoop($"{nameof(xServerFeature)}.xServerSeedRefresh", async token =>
            {
                await UpdatePeersAsync(this.xServerPeerList).ConfigureAwait(false);
                this.logger.LogInformation($"Saving refreshed xServer Seeds to {this.xServerPeerList.Path}");
                await this.xServerPeerList.Save().ConfigureAwait(false);
            },
            this.nodeLifetime.ApplicationStopping,
            repeatEvery: TimeSpan.FromSeconds(RefreshXServerRate),
            startAfter: TimeSpans.Second);
        }

        /// <inheritdoc />
        public void Stop()
        {
            this.xServerDiscoveryLoop?.Dispose();
            this.xServerRefreshLoop?.Dispose();
        }

        /// <inheritdoc />
        public RegisterResult RegisterXServer(RegisterRequest registerRequest)
        {
            var result = new RegisterResult();
            string xServerURL = Utils.GetServerUrl(registerRequest.NetworkProtocol, registerRequest.NetworkAddress, registerRequest.NetworkPort);
            var client = new RestClient(xServerURL);
            var registerRestRequest = new RestRequest("/registerserver", Method.POST);
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
        public List<PairResult> GetAvailablePairs()
        {
            var result = new List<PairResult>();
            var t3Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Three).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t3Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t3Node.NetworkProtocol, t3Node.NetworkAddress, t3Node.NetworkPort);
                var client = new RestClient(xServerURL);
                var getPairsRestRequest = new RestRequest("/getavailablepairs", Method.GET)
                {
                    RequestFormat = DataFormat.Json
                };

                var getPairResult = client.Execute<List<PairResult>>(getPairsRestRequest);
                if (getPairResult.StatusCode == HttpStatusCode.OK)
                {
                    result = getPairResult.Data;
                }
            }
            return result;
        }

        /// <inheritdoc />
        public PriceLockResult CreatePriceLock(CreatePriceLockRequest priceLockRequest)
        {
            var result = new PriceLockResult();
            var t3Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Three).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t3Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t3Node.NetworkProtocol, t3Node.NetworkAddress, t3Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var createPriceLockRequest = new RestRequest("/createpricelock", Method.POST);
                var request = JsonConvert.SerializeObject(priceLockRequest);
                createPriceLockRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
                createPriceLockRequest.RequestFormat = DataFormat.Json;

                var createPLResult = client.Execute<PriceLockResult>(createPriceLockRequest);
                if (createPLResult.StatusCode == HttpStatusCode.OK)
                {
                    result = createPLResult.Data;
                }
                else
                {
                    result.ResultMessage = "Failed to access xServer";
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 3 servers";
            }
            return result;
        }

        /// <inheritdoc />
        public PriceLockResult GetPriceLock(string priceLockId)
        {
            var result = new PriceLockResult();
            var t3Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Three).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t3Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t3Node.NetworkProtocol, t3Node.NetworkAddress, t3Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var getPriceLockRequest = new RestRequest("/getpricelock", Method.GET);
                getPriceLockRequest.AddParameter("priceLockId", priceLockId);

                var createPLResult = client.Execute<PriceLockResult>(getPriceLockRequest);
                if (createPLResult.StatusCode == HttpStatusCode.OK)
                {
                    result = createPLResult.Data;
                }
                else
                {
                    result.ResultMessage = "Failed to access xServer";
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 3 servers";
            }
            return result;
        }

        /// <inheritdoc />
        public SubmitPaymentResult SubmitPayment(SubmitPaymentRequest submitPaymentRequest)
        {
            var result = new SubmitPaymentResult();
            var t3Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Three).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t3Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t3Node.NetworkProtocol, t3Node.NetworkAddress, t3Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var paymentRequest = new RestRequest("/submitpayment", Method.POST);
                var request = JsonConvert.SerializeObject(submitPaymentRequest);
                paymentRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
                paymentRequest.RequestFormat = DataFormat.Json;

                var submitPaymentResult = client.Execute<SubmitPaymentResult>(paymentRequest);
                if (submitPaymentResult.StatusCode == HttpStatusCode.OK)
                {
                    result = submitPaymentResult.Data;
                }
                else
                {
                    result.ResultMessage = "Failed to access xServer";
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 3 servers";
            }
            return result;
        }

        /// <inheritdoc />
        public ProfileResult GetProfile(string name, string keyAddress)
        {
            var result = new ProfileResult();
            var t2Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t2Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var getPriceLockRequest = new RestRequest("/getprofile", Method.GET);
                getPriceLockRequest.AddParameter("name", name);
                getPriceLockRequest.AddParameter("keyAddress", keyAddress);

                var createPLResult = client.Execute<ProfileResult>(getPriceLockRequest);
                if (createPLResult.StatusCode == HttpStatusCode.OK)
                {
                    if (createPLResult.Data == null)
                    {
                        result.Success = false;
                    }
                    else
                    {
                        result = createPLResult.Data;
                        result.Success = true;
                    }
                }
                else
                {
                    result.ResultMessage = "Failed to access xServer";
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 2 servers";
            }
            return result;
        }

        /// <inheritdoc />
        public ReserveProfileResult ReserveProfile(ProfileReserveRequest reserveRequest)
        {
            var result = new ReserveProfileResult();
            var t2Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t2Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var reserveProfileRequest = new RestRequest("/reserveprofile", Method.POST);
                var request = JsonConvert.SerializeObject(reserveRequest);
                reserveProfileRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
                reserveProfileRequest.RequestFormat = DataFormat.Json;

                var reserveProfileResult = client.Execute<ReserveProfileResult>(reserveProfileRequest);
                if (reserveProfileResult.StatusCode == HttpStatusCode.OK)
                {
                    if (reserveProfileResult.Data == null)
                    {
                        result.Success = false;
                    }
                    else
                    {
                        result = reserveProfileResult.Data;
                        result.Success = true;
                    }
                }
                else
                {
                    result.ResultMessage = "Failed to access xServer";
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 2 servers";
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

        private void SyncPeerToPeersList(xServerPeers xServerPeerList, xServerPeer peer, bool seedCheck = false, bool removePeer = false)
        {

            var peersList = xServerPeerList.GetPeers();
            int peerIndex = peersList.FindIndex(p => p.NetworkAddress == peer.NetworkAddress);
            if (removePeer)
            {
                peersList.Remove(peer);
            }
            else if (seedCheck)
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
            lock (this.xServerPeersLock)
            {
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
                    NetworkAddress = networkAddress.NetworkAddress,
                    NetworkPort = networkAddress.NetworkPort,
                    Priority = -1,
                    Version = "N/A",
                    ResponseTime = 99999999,
                    Tier = (int)TierLevel.Seed
                };
                SyncPeerToPeersList(xServerPeerList, seedPeer, seedCheck: true);
            }
        }

        private async Task UpdatePeersAsync(xServerPeers xServerPeerList)
        {
            foreach (var peer in xServerPeerList.GetPeers())
            {
                string xServerURL = Utils.GetServerUrl(peer.NetworkProtocol, peer.NetworkAddress, peer.NetworkPort);
                var client = new RestClient(xServerURL);
                var pingRequest = new RestRequest("/ping/", Method.GET);
                var pingResponseTime = Stopwatch.StartNew();
                var pingResult = await client.ExecuteAsync<PingResult>(pingRequest).ConfigureAwait(false);
                pingResponseTime.Stop();
                if (pingResult.StatusCode == HttpStatusCode.OK)
                {
                    var pingData = pingResult.Data;
                    peer.Version = pingData.Version;
                    peer.ResponseTime = pingResponseTime.ElapsedMilliseconds;
                    peer.Tier = pingData.Tier;
                    SyncPeerToPeersList(xServerPeerList, peer);
                }
                else
                {
                    SyncPeerToPeersList(xServerPeerList, peer, removePeer: true);
                }
            }
        }

        private async Task XServerDiscoveryAsync(xServerPeers xServerPeerList)
        {
            var seedList = new ConcurrentBag<NetworkXServer>();
            int topResult = 10;
            await this.xServerSettings.RetrieveNodes().ForEachAsync(10, this.nodeLifetime.ApplicationStopping, async (peer, cancellation) =>
            {
                if (this.nodeLifetime.ApplicationStopping.IsCancellationRequested)
                    return;

                string xServerURL = Utils.GetServerUrl(peer.NetworkProtocol, peer.NetworkAddress, peer.NetworkPort);

                this.logger.LogDebug($"Attempting connection to {xServerURL}.");

                var client = new RestClient(xServerURL);
                var topXServersRequest = new RestRequest("/gettop/", Method.GET);
                topXServersRequest.AddParameter("top", topResult);
                var topXServerResult = await client.ExecuteAsync<TopResult>(topXServersRequest, cancellation).ConfigureAwait(false);
                if (topXServerResult.StatusCode == HttpStatusCode.OK)
                {
                    seedList.Add(peer);
                    if (topXServerResult.Data?.XServers?.Count > 0)
                    {
                        var xServers = topXServerResult.Data.XServers;
                        foreach (var xServer in xServers)
                        {
                            xServerURL = Utils.GetServerUrl(xServer.NetworkProtocol, xServer.NetworkAddress, xServer.NetworkPort);
                            client = new RestClient(xServerURL);
                            var pingRequest = new RestRequest("/ping/", Method.GET);
                            var pingResponseTime = Stopwatch.StartNew();
                            var pingResult = await client.ExecuteAsync<PingResult>(pingRequest, cancellation).ConfigureAwait(false);
                            pingResponseTime.Stop();
                            if (pingResult.StatusCode == HttpStatusCode.OK)
                            {
                                var ping = pingResult.Data;
                                var newPeer = new xServerPeer()
                                {
                                    Name = xServer.Name,
                                    NetworkProtocol = xServer.NetworkProtocol,
                                    NetworkAddress = xServer.NetworkAddress,
                                    NetworkPort = xServer.NetworkPort,
                                    Priority = xServer.Priotiry,
                                    Version = ping.Version,
                                    ResponseTime = pingResponseTime.ElapsedMilliseconds,
                                    Tier = ping.Tier
                                };
                                SyncPeerToPeersList(xServerPeerList, newPeer);
                            }
                        }
                    }
                }

                SyncSeedsToPeersList(xServerPeerList, seedList);

            }).ConfigureAwait(false);
        }
    }
}
