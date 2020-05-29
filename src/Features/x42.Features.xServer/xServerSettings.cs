using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Blockcore.Configuration;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using NBitcoin.Protocol;

namespace x42.Features.xServer
{
    /// <summary>
    /// Configuration related to storage of transactions.
    /// </summary>
    public class xServerSettings
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>List of end points that the node should try to connect to.</summary>
        /// <remarks>All access should be protected under <see cref="addxServerNodeLock"/></remarks>
        private readonly List<NetworkXServer> addxServerNode;

        /// <summary>
        /// Protects access to the list of addnode endpoints.
        /// </summary>
        private readonly object addxServerNodeLock;

        /// <summary>The network the node is running on.</summary>
        private readonly Network network;

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="nodeSettings">The node configuration.</param>
        /// <param name="network">The network managment.</param>
        public xServerSettings(NodeSettings nodeSettings, Network network)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));
            Guard.NotNull(network, nameof(network));

            this.logger = nodeSettings.LoggerFactory.CreateLogger(typeof(xServerSettings).FullName);
            this.network = network;

            TextFileConfiguration config = nodeSettings.ConfigReader;

            this.addxServerNodeLock = new object();

            lock (this.addxServerNodeLock)
            {
                this.addxServerNode = new List<NetworkXServer>();
            }

            try
            {
                foreach (NetworkXServer addNode in config.GetAll("addxservernode", this.logger).Select(c => c.ToIPXServerEndPoint(4242, false)))
                {
                    this.AddAddNode(addNode);
                }
            }
            catch (FormatException)
            {
                throw new ConfigurationException("Invalid 'addxservernode' parameter.");
            }

            AddSeedNodes();
        }

        public void AddAddNode(NetworkXServer addNode)
        {
            lock (this.addxServerNodeLock)
            {
                this.addxServerNode.Add(addNode);
            }
        }

        public void RemoveAddNode(NetworkXServer addNode)
        {
            lock (this.addxServerNodeLock)
            {
                this.addxServerNode.Remove(addNode);
            }
        }

        public List<NetworkXServer> RetrieveNodes()
        {
            lock (this.addxServerNodeLock)
            {
                return this.addxServerNode;
            }
        }

        /// <summary>
        /// Add peers to the address manager from the network's seed nodes.
        /// </summary>
        private void AddSeedNodes()
        {
            this.addxServerNode.AddRange(this.network.XServerSeedNodes);
        }


        /// <summary>Prints the help information on how to configure the block store settings to the logger.</summary>
        public static void PrintHelp(Network network)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"-addxservernode=<ip:port>        Add a xServer node to connect to and attempt to keep the connection open. Can be specified multiple times.");

            NodeSettings.Default(network).Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            builder.AppendLine("####xServer Settings####");
            builder.AppendLine($"#Add a xServer node to connect to and attempt to keep the connection open. Can be specified multiple times.");
            builder.AppendLine($"#addxservernode=<ip:port>");
        }
    }
}