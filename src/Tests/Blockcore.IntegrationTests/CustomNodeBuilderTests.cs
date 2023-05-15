using System;
using System.IO;
using Blockcore.Builder;
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
using Blockcore.Networks.Bitcoin;
using FluentAssertions;
using Xunit;

namespace Blockcore.IntegrationTests
{
    public class CustomNodeBuilderTests
    {
        private readonly Network network;

        public CustomNodeBuilderTests()
        {
            this.network = new BitcoinRegTest();
        }

        [Fact(Skip = "Investigate PeerConnector shutdown timeout issue")]
        public void CanUnderstandUnknownParams()
        {
            var extraParams = new NodeConfigParameters
            {
                { "some_new_unknown_param", "with a value" },
            };

            using (var nodeBuilder = NodeBuilder.Create(this))
            {
                var buildAction = new Action<IFullNodeBuilder>(builder =>
                    builder.UseBlockStore()
                        .UsePowConsensus()
                        .UseMempool()
                        .AddMining()
                        .UseWallet()
                        .AddRPC()
                        .UseNodeHost()
                        .MockIBD());

                var coreNode = nodeBuilder.CreateCustomNode(buildAction, this.network,
                    this.network.Consensus.ConsensusFactory.Protocol.ProtocolVersion, configParameters: extraParams);

                coreNode.Start();

                coreNode.ConfigParameters["some_new_unknown_param"].Should().Be("with a value");
            }
        }

        [Fact(Skip = "Investigate PeerConnector shutdown timeout issue")]
        public void CanUseCustomConfigFileFromParams()
        {
            var specialConf = "special.conf";

            var extraParams = new NodeConfigParameters
            {
                { "conf", specialConf },
            };

            using (var nodeBuilder = NodeBuilder.Create(this))
            {
                var buildAction = new Action<IFullNodeBuilder>(builder =>
                    builder.UseBlockStore()
                        .UsePowConsensus()
                        .UseMempool()
                        .AddMining()
                        .UseWallet()
                        .AddRPC()
                        .UseNodeHost()
                        .MockIBD());

                var coreNode = nodeBuilder.CreateCustomNode(buildAction, this.network, this.network.Consensus.ConsensusFactory.Protocol.ProtocolVersion, configParameters: extraParams);

                coreNode.Start();

                coreNode.ConfigParameters["conf"].Should().Be(specialConf);
                File.Exists(Path.Combine(coreNode.DataFolder, specialConf)).Should().BeTrue();
            }
        }
    }
}