using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.WebHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.ColdStaking;
using Blockcore.Features.Consensus;
using Blockcore.Features.Diagnostic;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;

namespace Blockcore.Node
{
    public static class NodeBuilder
    {
        public static IFullNodeBuilder Create(string chain, NodeSettings settings)
        {
            chain = chain.ToUpperInvariant();

            IFullNodeBuilder nodeBuilder = CreateBaseBuilder(chain, settings);

            switch (chain)
            {
                case "BTC":
                    nodeBuilder.UsePowConsensus().AddMining().UseWallet();
                    break;
                case "CITY":
                case "STRAT":
                case "X42":
                case "XDS":
                    nodeBuilder.UsePosConsensus().AddPowPosMining().UseColdStakingWallet();
                    break;
            }

            return nodeBuilder;
        }

        private static IFullNodeBuilder CreateBaseBuilder(string chain, NodeSettings settings)
        {
            IFullNodeBuilder nodeBuilder = new FullNodeBuilder()
            .UseNodeSettings(settings)
            .UseBlockStore()
            .UseMempool()
            .UseApi()
            .AddRPC()
            .UseDiagnosticFeature();

            return nodeBuilder;
        }
    }
}
