using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.NodeHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P;
using NBitcoin;
using Blockcore.Tests.Common;

namespace Blockcore.IntegrationTests.Common.Runners
{
    public sealed class StratisBitcoinPowRunner : NodeRunner
    {
        public StratisBitcoinPowRunner(string dataDir, Network network, string agent)
            : base(dataDir, agent)
        {
            this.Network = network;
        }

        public override void BuildNode()
        {
            NodeSettings settings = null;

            if (string.IsNullOrEmpty(this.Agent))
                settings = new NodeSettings(this.Network, args: new string[] { "-conf=bitcoin.conf", "-datadir=" + this.DataFolder });
            else
                settings = new NodeSettings(this.Network, agent: this.Agent, args: new string[] { "-conf=bitcoin.conf", "-datadir=" + this.DataFolder });

            var persistenceProviderManager = new TestPersistenceProviderManager(settings);


            var builder = new FullNodeBuilder()
                            .UsePersistenceProviderMananger(persistenceProviderManager)
                            .UseNodeSettings(settings)
                            .UseBlockStore()
                            .UsePowConsensus()
                            .UseMempool()
                            .AddMining()
                            .UseWallet()
                            .AddRPC()
                            .UseNodeHost()
                            .UseTestChainedHeaderTree()
                            .MockIBD();

            if (this.ServiceToOverride != null)
                builder.OverrideService<BaseFeature>(this.ServiceToOverride);

            if (!this.EnablePeerDiscovery)
            {
                builder.RemoveImplementation<PeerConnectorDiscovery>();
                builder.ReplaceService<IPeerDiscovery, BaseFeature>(new PeerDiscoveryDisabled());
            }

            if (this.AlwaysFlushBlocks)
            {
                builder.ReplaceService<IBlockStoreQueueFlushCondition, BlockStoreFeature>(new BlockStoreAlwaysFlushCondition());
            }

            this.FullNode = (FullNode)builder.Build();
        }
    }
}