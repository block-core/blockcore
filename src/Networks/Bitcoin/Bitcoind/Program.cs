using System;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.NodeHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Networks;
using Blockcore.Networks.Bitcoin;
using Blockcore.Utilities;
using Blockcore.Utilities.Store;

namespace BitcoinD
{
    public class Program
    {
#pragma warning disable IDE1006 // Naming Styles

        public static async Task Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Bitcoin, args: args);

                IFullNode node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePowConsensus()
                    .UseMempool()
                    .AddMining()
                    .AddRPC()
                    .UseWallet()
                    .UseNodeHost()
                    .Build();

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