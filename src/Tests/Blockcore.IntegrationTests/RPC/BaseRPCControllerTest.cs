using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.RPC;
using Blockcore.Networks;
using Blockcore.Networks.Bitcoin;
using Blockcore.Tests.Common;

namespace Blockcore.IntegrationTests.RPC
{
    /// <summary>
    /// Base class for RPC tests.
    /// </summary>
    public abstract class BaseRPCControllerTest : TestBase
    {
        protected BaseRPCControllerTest() : base(new BitcoinRegTest())
        {
        }

        /// <summary>
        /// Builds a node with basic services and RPC enabled.
        /// </summary>
        /// <param name="dir">Data directory that the node should use.</param>
        /// <returns>Interface to the newly built node.</returns>
        public IFullNode BuildServicedNode(string dir)
        {
            var nodeSettings = new NodeSettings(this.Network, args: new string[] { $"-datadir={dir}" });
            var persistenceProviderManager = new TestPersistenceProviderManager(nodeSettings);

            var fullNodeBuilder = new FullNodeBuilder(nodeSettings, persistenceProviderManager);
            IFullNode fullNode = fullNodeBuilder
                .UseBlockStore()
                .UsePowConsensus()
                .UseMempool()
                .AddRPC()
                .Build();

            return fullNode;
        }
    }
}