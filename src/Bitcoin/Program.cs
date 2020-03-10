using System;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Features.Api;
using Blockcore.Features.Miner;
using Blockcore.Utilities;

namespace Blockcore.Bitcoin
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Networks.Bitcoin, args: args);

                IFullNode node = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePowConsensus()
                    .UseMempool()
                    .AddMining()
                    .AddRPC()
                    .UseWallet()
                    .UseApi()
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