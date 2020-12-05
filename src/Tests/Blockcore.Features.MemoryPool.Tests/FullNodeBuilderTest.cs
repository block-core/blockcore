using System;
using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;

using Xunit;

namespace Blockcore.Features.MemoryPool.Tests
{
    public class FullNodeBuilderTest
    {
        [Fact]
        public void CanHaveAllFullnodeServicesTest()
        {
            // This test is put in the mempool feature because the
            // mempool requires all the features to be a fullnode.

            var nodeSettings = new NodeSettings(KnownNetworks.TestNet, args: new string[] {
                $"-datadir=Blockcore.Features.MemoryPool.Tests/TestData/FullNodeBuilderTest/CanHaveAllServicesTest" });

            var persistenceManager = new TestPersistenceProviderManager(nodeSettings);

            var fullNodeBuilder = new FullNodeBuilder(nodeSettings, persistenceManager);
            IFullNode fullNode = fullNodeBuilder
                .UseBlockStore()
                .UsePowConsensus()
                .UseMempool()
                .Build();

            IServiceProvider serviceProvider = fullNode.Services.ServiceProvider;
            var network = serviceProvider.GetService<Network>();
            var settings = serviceProvider.GetService<NodeSettings>();
            var consensusManager = serviceProvider.GetService<IConsensusManager>() as ConsensusManager;
            var chain = serviceProvider.GetService<ChainIndexer>();
            var chainState = serviceProvider.GetService<IChainState>() as ChainState;
            var consensusRuleEngine = serviceProvider.GetService<IConsensusRuleEngine>();
            consensusRuleEngine.SetupRulesEngineParent();
            var mempoolManager = serviceProvider.GetService<MempoolManager>();
            var connectionManager = serviceProvider.GetService<IConnectionManager>() as ConnectionManager;

            Assert.NotNull(fullNode);
            Assert.NotNull(network);
            Assert.NotNull(settings);
            Assert.NotNull(consensusManager);
            Assert.NotNull(chain);
            Assert.NotNull(chainState);
            Assert.NotNull(consensusRuleEngine);
            Assert.NotNull(mempoolManager);
            Assert.NotNull(connectionManager);
        }
    }
}