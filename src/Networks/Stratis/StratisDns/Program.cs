using System;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.NodeHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.Dns;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Networks.Stratis;
using Blockcore.Utilities;

namespace StratisDnsD
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The async entry point for the Stratis Dns process.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>A task used to await the operation.</returns>
        public static async Task Main(string[] args)
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Stratis, args: args);
                var dnsSettings = new DnsSettings(nodeSettings);

                if (string.IsNullOrWhiteSpace(dnsSettings.DnsHostName) || string.IsNullOrWhiteSpace(dnsSettings.DnsNameServer) || string.IsNullOrWhiteSpace(dnsSettings.DnsMailBox))
                    throw new ConfigurationException("When running as a DNS Seed service, the -dnshostname, -dnsnameserver and -dnsmailbox arguments must be specified on the command line.");

                // Run as a full node with DNS or just a DNS service?
                IFullNode node;
                if (dnsSettings.DnsFullNode)
                {
                    // Build the Dns full node.
                    node = new FullNodeBuilder()
                        .UseNodeSettings(nodeSettings)
                        .UseBlockStore()
                        .UsePosConsensus()
                        .UseMempool()
                        .UseWallet()
                        .AddPowPosMining()
                        .UseNodeHost()
                        .AddRPC()
                        .UseDns()
                        .Build();
                }
                else
                {
                    // Build the Dns node.
                    node = new FullNodeBuilder()
                        .UseNodeSettings(nodeSettings)
                        .UsePosConsensus()
                        .UseNodeHost()
                        .AddRPC()
                        .UseDns()
                        .Build();
                }

                // Run node.
                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }
    }
}