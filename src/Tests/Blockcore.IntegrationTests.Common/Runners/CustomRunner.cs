using System;
using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.Networks;
using Blockcore.P2P;
using Blockcore.Tests.Common;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.IntegrationTests.Common.Runners
{
    public sealed class CustomNodeRunner : NodeRunner
    {
        private readonly Action<IFullNodeBuilder> callback;
        private readonly uint protocolVersion;
        private readonly uint minProtocolVersion;
        private readonly NodeConfigParameters configParameters;

        public CustomNodeRunner(string dataDir, Action<IFullNodeBuilder> callback, Network network,
            uint protocolVersion, NodeConfigParameters configParameters = null, string agent = "Custom",
            uint minProtocolVersion = ProtocolVersion.SENDHEADERS_VERSION)
            : base(dataDir, agent)
        {
            this.callback = callback;
            this.Network = network;
            this.protocolVersion = protocolVersion;
            this.configParameters = configParameters ?? new NodeConfigParameters();
            this.minProtocolVersion = minProtocolVersion;
        }

        public override void BuildNode()
        {
            var argsAsStringArray = this.configParameters.AsConsoleArgArray();

            NodeSettings settings = null;

            if (string.IsNullOrEmpty(this.Agent))
                settings = new NodeSettings(this.Network, args: argsAsStringArray) { MinProtocolVersion = this.minProtocolVersion };
            else
                settings = new NodeSettings(this.Network, agent: this.Agent, args: argsAsStringArray) { MinProtocolVersion = this.minProtocolVersion };

            var persistenceProviderManager = new TestPersistenceProviderManager(settings);

            IFullNodeBuilder builder = new FullNodeBuilder()
                .UsePersistenceProviderMananger(persistenceProviderManager)
                .UseNodeSettings(settings);

            this.callback(builder);

            builder.RemoveImplementation<PeerConnectorDiscovery>();
            builder.ReplaceService<IPeerDiscovery, BaseFeature>(new PeerDiscoveryDisabled());

            this.FullNode = (FullNode)builder.Build();
        }
    }
}