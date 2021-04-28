using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.Consensus;
using Blockcore.Features.RPC;
using Blockcore.Tests.Common;

using Xunit;

namespace Blockcore.IntegrationTests.RPC
{
    public class RPCSettingsTest : TestBase
    {
        public RPCSettingsTest() : base(KnownNetworks.Main)
        {
        }

        [Fact]
        public void CanSpecifyRPCSettings()
        {
            string dir = CreateTestDir(this);

            var nodeSettings = new NodeSettings(this.Network, args: new string[] { $"-datadir={dir}", "-rpcuser=abc", "-rpcpassword=def", "-rpcport=91", "-server=1" });

            IFullNode node = new FullNodeBuilder()
                .UsePersistenceProviderMananger(new TestPersistenceProviderManager(nodeSettings))
                .UseNodeSettings(nodeSettings)
                .UsePowConsensus()
                .AddRPC()
                .Build();

            var settings = node.NodeService<RpcSettings>();

            Assert.Equal("abc", settings.RpcUser);
            Assert.Equal("def", settings.RpcPassword);
            Assert.Equal(91, settings.RPCPort);
        }
    }
}