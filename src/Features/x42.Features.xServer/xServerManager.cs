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
using System.Collections.Concurrent;
using NBitcoin;
using System.Net.Sockets;
using System.Linq;
using RestSharp.Serializers.NewtonsoftJson;
using Blockcore.Networks;
using Blockcore.Features.NodeHost.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Serialization;
using Renci.SshNet;
using System.Threading;

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
        /// 



        private readonly ISshManager sshManager;
        private readonly NodeHub nodeHub;



        public xServerManager(
            ILoggerFactory loggerFactory,
            DataFolder dataFolders,
            IAsyncProvider asyncProvider,
            INodeLifetime nodeLifetime,
            xServerSettings xServerSettings,
            Network network,
            ISshManager sshManager,
            NodeHub nodeHub)
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
            this.sshManager = sshManager;
            this.nodeHub = nodeHub;
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

        public async Task<bool> TestSshCredentials(TestSshCredentialRequest request)
        {

            return await this.sshManager.TestSshCredentialsAsync(request);

        }

        /// <inheritdoc />

        public async Task SetUpxServer(xServerProvisioningRequest request)
        {
            await this.nodeHub.Echo(" ");
            await this.nodeHub.Echo("*******************************************");
            await this.nodeHub.Echo("Welcome to the xServer provisioning tool!");
            await this.nodeHub.Echo("*******************************************");
            await this.nodeHub.Echo(" ");
            try
            {

                var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configPath = Path.Combine(appDataFolder, "Config");
                var configExists = Directory.Exists(configPath);
                if (!configExists)
                {

                    Directory.CreateDirectory(configPath);

                }

                if(!(await UpdateProfileDNSAsync(new ProfileDnsUpdateRequest() { Ip = request.IpAddress, Profile = request.Profile })))
                {

                    await this.nodeHub.Echo("An Error has Occured");


                }

                CopyConfigFiles(".env");
                CopyConfigFiles("app.config.json");
                CopyConfigFiles("xServer.conf");
                ReplaceVariable(".env", "profile", request.Profile.ToLower());
                ReplaceVariable(".env", "postgrespass", request.DatabasePassword);
                ReplaceVariable("app.config.json", "profile", request.Profile.ToLower());
                ReplaceVariable("xServer.conf", "postgrespass", request.DatabasePassword);

    

                await this.nodeHub.Echo("Connecting via ssh...");

                var commndList = new List<Models.SshCommand>();


                commndList.Add(new Models.SshCommand() { Command = "apt-get update -y", Description = "Updates" });
                commndList.Add(new Models.SshCommand() { Command = "apt-get install git -y", Description = "Install Git" });
                commndList.Add(new Models.SshCommand() { Command = "git clone https://github.com/x42protocol/x42-Server-Deployment", Description = "Clone Repository" });

                commndList.Add(new Models.SshCommand() { Command = "cd x42-Server-Deployment && sh setup.sh", Description = "Running Setup" });

                commndList.Add(new Models.SshCommand() { Command = "cd x42-Server-Deployment && cd traefik && sh create_ca.sh", Description = "Generating Certificate Authority" });

                await this.nodeHub.Echo("SSH Connection success!");
                await this.nodeHub.Echo("Installing xServer...");



                var scopedSshManager = new SshManager(request.IpAddress, request.SshUser, request.SsHPassword, this.nodeHub);

                foreach (var commandItem in commndList)
                {

                    await this.nodeHub.Echo($"Starting {commandItem.Description}...");

                    await scopedSshManager.ExecuteCommand(commandItem.Command);
                    Thread.Sleep(5000);

                    await this.nodeHub.Echo($"Finished {commandItem.Description}...");

                }


                using (var sftp = new SftpClient(request.IpAddress, request.SshUser, request.SsHPassword))
                {
                    sftp.Connect();
                    UploadFile(sftp, ".env", "xserver");
                    UploadFile(sftp, "app.config.json", "xserver/xserverui");
                    UploadFile(sftp, "xServer.conf", "xserver/xserver");

                    sftp.Disconnect();

                }

                var startTraefik = new Models.SshCommand() { Command = "cd x42-Server-Deployment && cd traefik && docker-compose up -d", Description = "Starting Traefik" };

                Thread.Sleep(5000);

                var issueClientCertificate = new Models.SshCommand() { Command = "cd x42-Server-Deployment && cd traefik && sh client_certificates.sh " + request.Profile + " " + request.CertificatePassword + " " + request.EmailAddress + "", Description = "Issuing Certificate" };

                var startDocker = new Models.SshCommand() { Command = "cd x42-Server-Deployment && cd xserver && docker-compose up -d", Description = "Starting xServer" };



                await this.nodeHub.Echo($"Starting {startTraefik.Description}...");

                await scopedSshManager.ExecuteCommand(startTraefik.Command);

                await this.nodeHub.Echo($"Finished {startTraefik.Description}...");


                await this.nodeHub.Echo($"Starting {issueClientCertificate.Description}...");

                await scopedSshManager.ExecuteCommand(issueClientCertificate.Command);

                await this.nodeHub.Echo($"Finished {issueClientCertificate.Description}...");


                await this.nodeHub.Echo($"Starting {startDocker.Description}...");

                await scopedSshManager.ExecuteCommand(startDocker.Command);

                await this.nodeHub.Echo($"Finished {startDocker.Description}...");

                using (var sftp = new SftpClient(request.IpAddress, request.SshUser, request.SsHPassword))
                {
                    sftp.Connect();
                    DownloadClientCertificate(sftp, request.Profile);
                    sftp.Disconnect();

                }


                await this.nodeHub.Echo("xServer installation Complete!");
            }
            catch (Exception e)
            {
                this.logger.LogError(e.Message);
                await this.nodeHub.Echo(e.Message);
                 

            }


             


        }

        private static void CopyConfigFiles(string fileName)
        {
            var x42MainFolder = Path.Combine(Environment.CurrentDirectory,"resources", "daemon", "AppData");
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);


            if (!Directory.Exists(Path.Combine(appDataFolder, "Config")))
            {

                Directory.CreateDirectory(Path.Combine(appDataFolder, "Config"));

            }
            string sourceFile = Path.Combine(x42MainFolder, fileName);
            string desitinationFile = Path.Combine(appDataFolder, "Config", fileName);
            if (File.Exists(desitinationFile)) {

                File.Delete(desitinationFile);
            }

            File.Copy(sourceFile, desitinationFile);
        }

        private static void ReplaceVariable(string fileName, string variable, string value)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            string pathString = Path.Combine(appDataFolder, "Config", fileName);
             

            string text = File.ReadAllText(pathString);
            text = text.Replace("{" + variable + "}", value);
            if (File.Exists(pathString)) {
                File.Delete(pathString);
            }
            File.WriteAllText(pathString, text);
        }

        private static void UploadFile(SftpClient sftp, string fileName, string destination)
        {

            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);


            string pathString = Path.Combine(appDataFolder, "Config");


            using (FileStream filestream = File.OpenRead(Path.Combine(pathString, fileName)))
            {
                sftp.UploadFile(filestream, "/" + "/root/x42-Server-Deployment/" + destination + "/" + fileName, null);

            }
        }

        private static void DownloadClientCertificate(SftpClient sftp, string profileName)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var x42MainFolder = appDataFolder + "\\Blockcore\\x42\\x42Main";


            string pathString = Path.Combine(x42MainFolder,"certificates");

            if (!Directory.Exists(pathString))
            {

                Directory.CreateDirectory(pathString);

            }
            using (Stream file1 = File.OpenWrite(pathString + "\\" + profileName + ".p12"))
            {
                sftp.DownloadFile("/root/x42-Server-Deployment/traefik/pki/" + profileName + "/" + profileName + ".p12", file1);
            }



        }

        /// <inheritdoc />
        public ServerRegisterResult SearchForXServer(string profileName = "", string signAddress = "")
        {
            var result = new ServerRegisterResult();
            var xservers = this.xServerPeerList.GetPeers().OrderBy(n => n.ResponseTime);
            foreach (var xserver in xservers)
            {
                string xServerURL = Utils.GetServerUrl(xserver.NetworkProtocol, xserver.NetworkAddress, xserver.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var searchForXServerRequest = new RestRequest("/searchforxserver", Method.Get);
                searchForXServerRequest.AddParameter("profileName", profileName);
                searchForXServerRequest.AddParameter("signAddress", signAddress);

                var registerResult = client.ExecuteAsync<ServerRegisterResult>(searchForXServerRequest).Result;
                if (registerResult.StatusCode == HttpStatusCode.OK)
                {
                    result = registerResult.Data;
                    if (result.Id > 0)
                    {
                        result.Success = true;
                        return result;
                    }
                    else
                    {
                        result.Success = false;
                        result.ResultMessage = "No xServers found";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(result.ResultMessage))
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(registerResult.Content);
                        if (errorResponse != null)
                        {
                            result.ResultMessage = errorResponse.errors[0].message;
                        }
                        else
                        {
                            result.ResultMessage = "Failed to access xServer";
                        }
                    }
                    result.Success = false;
                }
            }
            return result;
        }

        /// <inheritdoc />
        public RegisterResult RegisterXServer(RegisterRequest registerRequest)
        {
            var result = new RegisterResult();
            string xServerURL = Utils.GetServerUrl(registerRequest.NetworkProtocol, registerRequest.NetworkAddress, registerRequest.NetworkPort);
            var client = new RestClient(xServerURL);
            var registerRestRequest = new RestRequest("/registerserver", Method.Post);
            registerRestRequest.AddBody(registerRequest);


            var registerResult = client.ExecuteAsync<RegisterResult>(registerRestRequest).Result;
            if (registerResult.StatusCode == HttpStatusCode.OK)
            {
                result = registerResult.Data;
            }
            else
            {
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(registerResult.Content);
                if (errorResponse != null)
                {
                    result.ResultMessage = errorResponse.errors[0].message;
                }
                else
                {
                    result.ResultMessage = "Failed to access xServer";
                }
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
                var getPairsRestRequest = new RestRequest("/getavailablepairs", Method.Get)
                {
                    RequestFormat = DataFormat.Json
                };

                var getPairResult = client.ExecuteAsync<List<PairResult>>(getPairsRestRequest).Result;
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
            var t3Nodes = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Three).OrderBy(n => n.ResponseTime);
            foreach (var t3Node in t3Nodes)
            {
                string xServerURL = Utils.GetServerUrl(t3Node.NetworkProtocol, t3Node.NetworkAddress, t3Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var createPriceLockRequest = new RestRequest("/createpricelock", Method.Post);

                createPriceLockRequest.AddBody(priceLockRequest);

                var createPLResult = client.ExecuteAsync<PriceLockResult>(createPriceLockRequest).Result;
                if (createPLResult.StatusCode == HttpStatusCode.OK)
                {
                    result = createPLResult.Data;
                    if (result.Success)
                    {
                        return result;
                    }
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(createPLResult.Content);
                    if (errorResponse != null)
                    {
                        result.ResultMessage = errorResponse.errors[0].message;
                    }
                    else
                    {
                        result.ResultMessage = "Failed to access xServer";
                    }
                    result.Success = false;
                }
            }
            if (t3Nodes.Count() == 0)
            {
                result.ResultMessage = "Not connected to any tier 3 servers";
            }
            return result;
        }

        /// <inheritdoc />
        public PriceLockResult GetPriceLock(string priceLockId)
        {
            var result = new PriceLockResult();
            var t3Nodes = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Three).OrderBy(n => n.ResponseTime);
            foreach (var t3Node in t3Nodes)
            {
                string xServerURL = Utils.GetServerUrl(t3Node.NetworkProtocol, t3Node.NetworkAddress, t3Node.NetworkPort);
                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var getPriceLockRequest = new RestRequest("/getpricelock", Method.Get);
                getPriceLockRequest.AddParameter("priceLockId", priceLockId);

                var createPLResult = client.ExecuteAsync<PriceLockResult>(getPriceLockRequest).Result;
                if (createPLResult.StatusCode == HttpStatusCode.OK)
                {
                    result = createPLResult.Data;
                    return result;
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(createPLResult.Content);
                    if (errorResponse != null)
                    {
                        result.ResultMessage = errorResponse.errors[0].message;
                    }
                    else
                    {
                        result.ResultMessage = "Failed to access xServer";
                    }
                    result.Success = false;
                }
            }
            if (t3Nodes.Count() == 0)
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
                var paymentRequest = new RestRequest("/submitpayment", Method.Post);
                paymentRequest.AddBody(submitPaymentRequest);

                var submitPaymentResult = client.ExecuteAsync<SubmitPaymentResult>(paymentRequest).Result;
                if (submitPaymentResult.StatusCode == HttpStatusCode.OK)
                {
                    result = submitPaymentResult.Data;
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(submitPaymentResult.Content);
                    if (errorResponse != null)
                    {
                        result.ResultMessage = errorResponse.errors[0].message;
                    }
                    else
                    {
                        result.ResultMessage = "Failed to access xServer";
                    }
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
            var t2Nodes = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two).OrderBy(n => n.ResponseTime).Take(3).ToList();

            foreach (var t2Node in t2Nodes)
            {
                if (t2Node != null)
                {
                    string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);
                    var client = new RestClient(xServerURL);
                    client.UseNewtonsoftJson();
                    var getPriceLockRequest = new RestRequest("/getprofile", Method.Get);
                    getPriceLockRequest.AddParameter("name", name);
                    getPriceLockRequest.AddParameter("keyAddress", keyAddress);

                    var createPLResult = client.ExecuteAsync<ProfileResult>(getPriceLockRequest).Result;
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
                            return result;

                        }
                    }
                    else
                    {
                        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(createPLResult.Content);
                        if (errorResponse != null)
                        {
                            result.ResultMessage = errorResponse.errors[0].message;
                        }
                        else
                        {
                            result.ResultMessage = "Failed to access xServer";
                        }
                        result.Success = false;
                    }
                }
                else
                {
                    result.ResultMessage = "Not connected to any tier 2 servers";
                }

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

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var reserveProfileRequest = new RestRequest("/reserveprofile", Method.Post);
                var request = JsonConvert.SerializeObject(reserveRequest);
                reserveProfileRequest.AddJsonBody(reserveRequest);

                reserveProfileRequest.RequestFormat = DataFormat.Json;

                var reserveProfileResult = client.ExecuteAsync<ReserveProfileResult>(reserveProfileRequest).Result;



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
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(reserveProfileResult.Content);
                    if (errorResponse != null)
                    {
                        result.ResultMessage = errorResponse.errors[0].message;
                    }
                    else
                    {
                        result.ResultMessage = "Failed to access xServer";
                    }
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 2 servers";
            }
            return result;
        }

        public async Task<List<string>> GetWordPressPreviewDomainsAsync()
        {

            var t2Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two && n.NetworkAddress.Contains("144.91.95.234")).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t2Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var reserveWordPressRequest = new RestRequest("/wordpresspreviewdomains", Method.Get);

                reserveWordPressRequest.RequestFormat = DataFormat.Json;

                var response = client.ExecuteAsync<List<string>>(reserveWordPressRequest).Result;

                return response.Data;
            }
            return new List<string>();
        }


        public async Task<bool> UpdateProfileDNSAsync(ProfileDnsUpdateRequest request)
        {
            var t2Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two && n.NetworkAddress.Contains("144.91.95.234")).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t2Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var updateDnsRequest = new RestRequest("/UpdateProfileDNS", Method.Post);
                updateDnsRequest.AddJsonBody(request);

                updateDnsRequest.RequestFormat = DataFormat.Json;

                var updateDnsResult = await client.ExecuteAsync(updateDnsRequest);

                if (updateDnsResult.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                
            }
            return false;
            
         }

        public ReserveWordPressResult ReserveWordpressPreviewDomain(WordPressReserveRequest wordpressrequest)
        {
            var result = new ReserveWordPressResult();
            var t2Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two && n.NetworkAddress.Contains("144.91.95.234")).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t2Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var client = new RestClient(xServerURL);
                client.UseNewtonsoftJson();
                var reserveWordPressRequest = new RestRequest("/reservewordpresspreviewDNS", Method.Post);
                var request = JsonConvert.SerializeObject(wordpressrequest);
                reserveWordPressRequest.AddJsonBody(wordpressrequest);

                reserveWordPressRequest.RequestFormat = DataFormat.Json;

                var reserveWordpressResult = client.ExecuteAsync<ReserveWordPressResult>(reserveWordPressRequest).Result;



                if (reserveWordpressResult.StatusCode == HttpStatusCode.OK)
                {
                    if (reserveWordpressResult.Data == null)
                    {
                        result.Success = false;
                    }
                    else
                    {
                        result = reserveWordpressResult.Data;
                        result.Success = true;
                    }
                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(reserveWordpressResult.Content);
                    if (errorResponse != null)
                    {
                        result.ResultMessage = errorResponse.errors[0].message;
                    }
                    else
                    {
                        result.ResultMessage = "Failed to access xServer";
                    }
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 2 servers";
            }
            return result;
        }

        public async Task ProvisionWordPressAsync(ProvisionWordPressRequest provisionWordPressRequest)
        {

    
            var result = new ReserveWordPressResult();
            var t2Node = this.xServerPeerList.GetPeers().Where(n => n.Tier == (int)TierLevel.Two && n.NetworkAddress.Contains("144.91.95.234")).OrderBy(n => n.ResponseTime).FirstOrDefault();
            if (t2Node != null)
            {
                string xServerURL = Utils.GetServerUrl(t2Node.NetworkProtocol, t2Node.NetworkAddress, t2Node.NetworkPort);

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var options = new RestClientOptions(xServerURL)
                {
                    ThrowOnAnyError = true,
                    Timeout = 120000  // 1 second. or whatever time you want.
                };
                var client = new RestClient(options);
                 

                client.UseNewtonsoftJson();
                var reserveWordPressRequest = new RestRequest("/provisionWordPress", Method.Post);
                var request = JsonConvert.SerializeObject(provisionWordPressRequest);
                reserveWordPressRequest.AddJsonBody(provisionWordPressRequest);

                reserveWordPressRequest.RequestFormat = DataFormat.Json;

                var reserveWordpressResult = await client.ExecuteAsync(reserveWordPressRequest);



                if (reserveWordpressResult.StatusCode == HttpStatusCode.OK)
                {
                    result.Success = true;

                }
                else
                {
                    var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(reserveWordpressResult.Content);
                    if (errorResponse != null)
                    {
                        result.ResultMessage = errorResponse.errors[0].message;
                    }
                    else
                    {
                        result.ResultMessage = "Failed to access xServer";
                    }
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "Not connected to any tier 2 servers";
            }
            await Task.FromResult(result);
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
                var xServersPingRequest = new RestRequest("/ping", Method.Get);
                var xServerPingResult = client.ExecuteAsync<PingResult>(xServersPingRequest).Result;
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
                var pingRequest = new RestRequest("/ping/", Method.Get);
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
                var topXServersRequest = new RestRequest("/gettop/", Method.Get);
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
                            var pingRequest = new RestRequest("/ping/", Method.Get);
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
