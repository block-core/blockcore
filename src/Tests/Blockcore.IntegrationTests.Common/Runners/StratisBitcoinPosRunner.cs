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
using Blockcore.Networks;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.Protocol;
using Blockcore.Utilities.Store;
using Blockcore.Tests.Common;

namespace Blockcore.IntegrationTests.Common.Runners
{
    public sealed class StratisBitcoinPosRunner : NodeRunner
    {
        private readonly bool isGateway;

        public StratisBitcoinPosRunner(string dataDir, Network network, string agent = "StratisBitcoin", bool isGateway = false)
            : base(dataDir, agent)
        {
            this.Network = network;
            this.isGateway = isGateway;
        }

        public override void BuildNode()
        {
            var settings = new NodeSettings(this.Network, this.Agent, args: new string[] { "-conf=stratis.conf", "-datadir=" + this.DataFolder });
            var persistenceProviderManager = new TestPersistenceProviderManager(settings);

            // For stratisX tests we need the minimum protocol version to be 70000.
            settings.MinProtocolVersion = ProtocolVersion.POS_PROTOCOL_VERSION;

            var builder = new FullNodeBuilder()
                .UsePersistenceProviderMananger(persistenceProviderManager)
                .UseNodeSettings(settings)
                .UseBlockStore()
                .UsePosConsensus()
                .UseMempool()
                .UseWallet()
                .AddPowPosMining()
                .AddRPC()
                .UseNodeHost()
                .UseTestChainedHeaderTree()
                .MockIBD();

            if (this.OverrideDateTimeProvider)
                builder.OverrideDateTimeProviderFor<MiningFeature>();

            if (!this.EnablePeerDiscovery)
            {
                builder.RemoveImplementation<PeerConnectorDiscovery>();
                builder.ReplaceService<IPeerDiscovery, BaseFeature>(new PeerDiscoveryDisabled());
            }

            this.FullNode = (FullNode)builder.Build();
        }

        /// <summary>
        /// Builds a node with POS miner and RPC enabled.
        /// </summary>
        /// <param name="dataDir">Data directory that the node should use.</param>
        /// <param name="staking">Flag to signal that the node should the start staking on start up or not.</param>
        /// <returns>Interface to the newly built node.</returns>
        /// <remarks>Currently the node built here does not actually stake as it has no coins in the wallet,
        /// but all the features required for it are enabled.</remarks>
        public static IFullNode BuildStakingNode(string dataDir, bool staking = true)
        {
            var nodeSettings = new NodeSettings(networksSelector: Networks.Stratis.Networks.Stratis, args: new string[] { $"-datadir={dataDir}", $"-stake={(staking ? 1 : 0)}", "-walletname=dummy", "-walletpassword=dummy" });
            var persistenceProviderManager = new TestPersistenceProviderManager(nodeSettings);

            var fullNodeBuilder = new FullNodeBuilder(nodeSettings, persistenceProviderManager);
            IFullNode fullNode = fullNodeBuilder
                                .UseBlockStore()
                                .UsePosConsensus()
                                .UseMempool()
                                .UseWallet()
                                .AddPowPosMining()
                                .AddRPC()
                                .MockIBD()
                                .UseTestChainedHeaderTree()
                                .Build();

            return fullNode;
        }
    }
}