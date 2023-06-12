using Blockcore.NBitcoin.Protocol;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol.Payloads;
using Xunit;

namespace Blockcore.Tests.P2P
{
    public sealed class NetworkPeerTests
    {
        public NetworkPeerTests()
        {
        }

        [Fact]
        public void NetworkPeerRequirementCheckForOutboundWithValidVersionAndValidServiceReturnsTrue()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.POS_PROTOCOL_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.True(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Network, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, false, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForOutboundWithValidVersionAndInvalidServiceReturnsFalse()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.POS_PROTOCOL_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.False(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Nothing, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, false, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForOutboundWithInvalidVersionAndValidServiceReturnsFalse()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.SENDHEADERS_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.False(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Network, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, false, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForOutboundWithInvalidVersionAndInvalidServiceReturnsFalse()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.SENDHEADERS_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.False(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Nothing, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, false, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForInboundWithValidVersionAndValidServiceReturnsTrue()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.POS_PROTOCOL_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.True(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Network, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, true, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForInboundWithValidVersionAndInvalidServiceReturnsTrue()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.POS_PROTOCOL_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.True(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Nothing, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, true, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForInboundWithInvalidVersionAndValidServiceReturnsFalse()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.SENDHEADERS_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.False(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Network, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, true, out string reason));
        }

        [Fact]
        public void NetworkPeerRequirementCheckForInboundWithInvalidVersionAndInvalidServiceReturnsFalse()
        {
            NetworkPeerRequirement networkPeerRequirement = new NetworkPeerRequirement();
            networkPeerRequirement.MinVersion = ProtocolVersion.SENDHEADERS_VERSION;
            networkPeerRequirement.RequiredServices = NetworkPeerServices.Network;
            Assert.False(networkPeerRequirement.Check(new VersionPayload() { Services = NetworkPeerServices.Nothing, Version = ProtocolVersion.POS_PROTOCOL_VERSION }, true, out string reason));
        }
    }
}