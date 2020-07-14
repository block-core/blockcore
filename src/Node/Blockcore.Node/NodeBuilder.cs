using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.BlockStore;
using Blockcore.Features.ColdStaking;
using Blockcore.Features.Consensus;
using Blockcore.Features.Diagnostic;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Features.NodeHost;
using Blockcore.Features.Dns;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Blockcore.Features.Storage;

namespace Blockcore.Node
{
    public enum Mode
    {
        Full,
        DNS,
        Storage
    }

    public static class NodeBuilder
    {
        public static IFullNodeBuilder Create(string chain, NodeSettings settings, Mode mode)
        {
            chain = chain.ToUpperInvariant();

            IFullNodeBuilder nodeBuilder = CreateBaseBuilder(chain, settings, mode);

            if (mode == Mode.Full || mode == Mode.DNS)
            {
                switch (chain)
                {
                    case "BTC":
                        nodeBuilder.UsePowConsensus().AddMining().UseWallet();
                        break;
                    case "CITY":
                    case "STRAT":
                    case "RUTA":
                    case "EXOS":
                    case "X42":
                    case "XDS":
                        nodeBuilder.UsePosConsensus().AddPowPosMining().UseColdStakingWallet();
                        break;
                }
            }

            if (mode == Mode.Storage)
            {
                nodeBuilder.UsePosConsensus();
            }

            return nodeBuilder;
        }

        private static IFullNodeBuilder CreateBaseBuilder(string chain, NodeSettings settings, Mode mode)
        {
            IFullNodeBuilder nodeBuilder = null;

            if (mode == Mode.Full || mode == Mode.DNS)
            {
                nodeBuilder = new FullNodeBuilder()
                .UseNodeSettings(settings)
                .UseBlockStore()
                .UseMempool()
                .UseNodeHost()
                .AddRPC()
                .UseDiagnosticFeature();
            }

            if (mode == Mode.Storage)
            {
                nodeBuilder = new FullNodeBuilder()
                .UseNodeSettings(settings)
                .UseBlockStore()
                .UseNodeHost()
                .UseStorage()
                .UseDiagnosticFeature();
            }

            if (mode == Mode.DNS)
            {
                UseDnsFullNode(nodeBuilder, settings);
            }

            return nodeBuilder;
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
