using System;
using System.Threading.Tasks;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.NodeHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.ColdStaking;
using Blockcore.Features.Consensus;
using Blockcore.Features.Diagnostic;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Networks.Xds;
using Blockcore.Utilities;
using NBitcoin.Protocol;
using Blockcore.Utilities.Store;
using Blockcore.Features.Persistence.LevelDb;
using Blockcore.Features.Persistence.Rocksdb;

namespace StratisD
{
    public class Program
    {
#pragma warning disable IDE1006 // Naming Styles

        public static async Task Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Xds, args: args);
                var persistenceProviderManager = new PersistenceProviderManager(nodeSettings,
                    new LevelDbPersistenceProvider(),
                    new RocksDbPersistenceProvider()
                    // append additional persistence providers here
                    );

                IFullNodeBuilder nodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings, persistenceProviderManager)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseColdStakingWallet()
                    .AddPowPosMining()
                    .UseNodeHost()
                    .AddRPC()
                    .UseDiagnosticFeature();

                await nodeBuilder.Build().RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }
    }
}