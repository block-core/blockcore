using System;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.NodeHost;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.RPC;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.NBitcoin.Protocol;
using Blockcore.Networks;
using Blockcore.Networks.Stratis;
using Xunit;

namespace Blockcore.IntegrationTests
{
    public class ProvenHeaderTests
    {
        /// <summary>
        /// Prevent network being matched by name and replaced with a different network
        /// in the <see cref="NodeSettings" /> constructor.
        /// </summary>
        public class StratisOverrideRegTest : StratisRegTest
        {
            public StratisOverrideRegTest(string name = null) : base()
            {
                this.Name = name ?? Guid.NewGuid().ToString();
            }
        }

        public CoreNode CreateNode(NodeBuilder nodeBuilder, string agent, uint version = ProtocolVersion.POS_PROTOCOL_VERSION, NodeConfigParameters configParameters = null)
        {
            var callback = new Action<IFullNodeBuilder>(builder => builder
                .UseBlockStore()
                .UsePosConsensus()
                .UseMempool()
                .AddRPC()
                .UseNodeHost()
                .UseTestChainedHeaderTree()
                .MockIBD()
                );

            return nodeBuilder.CreateCustomNode(callback, new StratisOverrideRegTest(), ProtocolVersion.PROVEN_HEADER_VERSION, agent: agent, configParameters: configParameters);
        }

        /// <summary>
        /// Tests that a slot is reserved for at least one PH enabled peer.
        /// </summary>
        [Fact(Skip = "WIP")]
        public void LegacyNodesConnectsToProvenHeaderEnabledNode_AndLastOneIsDisconnectedToReserveSlot()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this).WithLogsEnabled())
            {
                // Create separate network parameters for this test.
                CoreNode phEnabledNode = this.CreateNode(builder, "ph-enabled", ProtocolVersion.PROVEN_HEADER_VERSION, new NodeConfigParameters { { "maxoutboundconnections", "3" } }).Start();
                CoreNode legacyNode1 = this.CreateNode(builder, "legacy1", ProtocolVersion.POS_PROTOCOL_VERSION).Start();
                CoreNode legacyNode2 = this.CreateNode(builder, "legacy2", ProtocolVersion.POS_PROTOCOL_VERSION).Start();
                CoreNode legacyNode3 = this.CreateNode(builder, "legacy3", ProtocolVersion.POS_PROTOCOL_VERSION).Start();

                TestHelper.Connect(phEnabledNode, legacyNode1);
                TestHelper.Connect(phEnabledNode, legacyNode2);
                TestHelper.Connect(phEnabledNode, legacyNode3);

                // TODO: ProvenHeadersReservedSlotsBehavior kicks in only during peer discovery, so it doesn't trigger when we have an inbound connection or
                // when we are using addnode/connect.
                // We need to configure a peers.json file or mock the PeerDiscovery to let phEnabledNode try to connect to legacyNode1, legacyNode2 and legacyNode3
                // With a maxoutboundconnections = 3, we expect the 3rd peer being disconnected to reserve a slot for a ph enabled node.

                // Assert.Equal(phEnabledNode.FullNode.ConnectionManager.ConnectedPeers.Count() == 2);
            }
        }
    }
}