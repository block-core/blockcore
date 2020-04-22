﻿using System;
using System.Net.Sockets;
using Blockcore.Connection;
using Blockcore.Controllers;
using Blockcore.IntegrationTests.Common.Extensions;

using Xunit;

namespace Blockcore.IntegrationTests.RPC
{
    public class AddNodeActionTest : BaseRPCControllerTest
    {
        [Fact]
        public void CanCall_AddNode()
        {
            string testDirectory = CreateTestDir(this);

            IFullNode fullNode = this.BuildServicedNode(testDirectory);
            fullNode.Start();

            var controller = fullNode.NodeController<ConnectionManagerRPCController>();

            Assert.True(controller.AddNode("0.0.0.0", "add"));
            Assert.Throws<ArgumentException>(() => { controller.AddNode("0.0.0.0", "notarealcommand"); });
            Assert.ThrowsAny<SocketException>(() => { controller.AddNode("a.b.c.d", "onetry"); });
            Assert.True(controller.AddNode("0.0.0.0", "remove"));
        }

        [Fact]
        public void CanCall_AddNode_AddsNodeToCollection()
        {
            string testDirectory = CreateTestDir(this);

            IFullNode fullNode = this.BuildServicedNode(testDirectory);

            var controller = fullNode.NodeController<ConnectionManagerRPCController>();

            var connectionManager = fullNode.NodeService<IConnectionManager>();
            controller.AddNode("0.0.0.0", "add");
            Assert.Single(connectionManager.ConnectionSettings.RetrieveAddNodes());
        }
    }
}