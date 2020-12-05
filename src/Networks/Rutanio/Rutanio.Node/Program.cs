using System;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.NodeHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.ColdStaking;
using Blockcore.Features.Consensus;
using Blockcore.Features.Dns;
using Blockcore.Features.Diagnostic;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Utilities;

namespace Rutanio.Daemon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Networks.Rutanio, args: args);

                IFullNodeBuilder nodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .AddPowPosMining()
                    .UseColdStakingWallet()
                    .UseDiagnosticFeature()
                    .UseNodeHost()
                    .AddRPC();

                // Build the Dns full node if enabled
                UseDnsFullNode(nodeBuilder, nodeSettings);

                IFullNode node = nodeBuilder.Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex);
            }
        }
        static void UseDnsFullNode(IFullNodeBuilder nodeBuilder, NodeSettings nodeSettings)
        {
            if (nodeSettings.ConfigReader.GetOrDefault("dnsfullnode", false, nodeSettings.Logger))
            {
                var dnsSettings = new DnsSettings(nodeSettings);

                if (string.IsNullOrWhiteSpace(dnsSettings.DnsHostName) || string.IsNullOrWhiteSpace(dnsSettings.DnsNameServer) || string.IsNullOrWhiteSpace(dnsSettings.DnsMailBox))
                    throw new ConfigurationException("When running as a DNS Seed service, the -dnshostname, -dnsnameserver and -dnsmailbox arguments must be specified on the command line.");

                nodeBuilder.UseDns();
            }
        }
    }
}