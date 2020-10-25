using System;
using System.Threading.Tasks;
using Blockcore;
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
using Blockcore.Utilities;
using Blockcore.Features.WalletWatchOnly;
using Blockcore.Features.Notifications;
using Blockcore.Utilities.Store;
using Blockcore.Features.Persistence.LevelDb;
using Blockcore.Features.Persistence.Rocksdb;

namespace x42.Daemon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Networks.x42, args: args);
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
                    .UseBlockNotification()
                    .UseTransactionNotification()
                    .UseColdStakingWallet()
                    .UseWatchOnlyWallet()
                    .AddPowPosMining()
                    .UseNodeHost()
                    .AddRPC()
                    .UseDiagnosticFeature();

                IFullNode node = nodeBuilder.Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex);
            }
        }
    }
}