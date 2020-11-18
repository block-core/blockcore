﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Configuration.Logging;
using Blockcore.Configuration.Settings;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Features.Consensus.Behaviors;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Networks.Stratis;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Signals;
using Blockcore.Tests.Common.Logging;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Consensus.Tests.ProvenBlockHeaders
{
    public sealed class ProvenHeaderConsenusManagerBehaviorTests : LogsTestBase
    {
        private readonly IChainState chainState;
        private readonly ICheckpoints checkpoints;
        private readonly ConnectionManagerSettings connectionManagerSettings;
        private readonly ILoggerFactory extendedLoggerFactory;
        private readonly IInitialBlockDownloadState initialBlockDownloadState;
        private readonly IPeerBanning peerBanning;
        private readonly IProvenBlockHeaderStore provenBlockHeaderStore;
        private readonly ISignals signals;
        private readonly IAsyncProvider asyncProvider;

        public ProvenHeaderConsenusManagerBehaviorTests() : base(new StratisTest())
        {
            this.chainState = new Mock<IChainState>().Object;
            this.checkpoints = new Mock<ICheckpoints>().Object;
            this.connectionManagerSettings = new ConnectionManagerSettings(NodeSettings.Default(this.Network));
            this.extendedLoggerFactory = ExtendedLoggerFactory.Create();
            this.initialBlockDownloadState = new Mock<IInitialBlockDownloadState>().Object;
            this.peerBanning = new Mock<IPeerBanning>().Object;
            this.provenBlockHeaderStore = new Mock<IProvenBlockHeaderStore>().Object;

            this.signals = new Signals.Signals(this.extendedLoggerFactory, null);
            this.asyncProvider = new AsyncProvider(this.extendedLoggerFactory, this.signals, new Mock<INodeLifetime>().Object);
        }

        private Mock<INetworkPeer> CreatePeerMock()
        {
            var peer = new Mock<INetworkPeer>();

            var connection = new NetworkPeerConnection(this.Network, peer.Object, new TcpClient(), 0, (message, token) => Task.CompletedTask, DateTimeProvider.Default, this.extendedLoggerFactory, new PayloadProvider(), this.asyncProvider);

            peer.SetupGet(networkPeer => networkPeer.Connection).Returns(connection);

            var connectionParameters = new NetworkPeerConnectionParameters();
            VersionPayload version = connectionParameters.CreateVersion(new IPEndPoint(1, 1), new IPEndPoint(1, 1), this.Network, DateTimeProvider.Default.GetTimeOffset());
            version.Services = NetworkPeerServices.Network;

            peer.SetupGet(x => x.PeerVersion).Returns(version);
            peer.SetupGet(x => x.State).Returns(NetworkPeerState.HandShaked);

            var stateChanged = new AsyncExecutionEvent<INetworkPeer, NetworkPeerState>();
            var messageReceived = new AsyncExecutionEvent<INetworkPeer, IncomingMessage>();

            peer.Setup(x => x.StateChanged).Returns(() => stateChanged);
            peer.Setup(x => x.MessageReceived).Returns(() => messageReceived);

            var connectionManagerBehaviorMock = new Mock<IConnectionManagerBehavior>();

            peer.Setup(x => x.Behavior<IConnectionManagerBehavior>()).Returns(() => connectionManagerBehaviorMock.Object);

            peer.SetupGet(x => x.PeerEndPoint).Returns(new IPEndPoint(1, 1));

            return peer;
        }

        [Fact]
        public void ConstructProvenHeaderPayload_Consecutive_Headers()
        {
            var provenHeaderChain = BuildProvenHeaderChain(10);

            var chain = new ChainIndexer(this.Network, provenHeaderChain);

            var consensusManager = new Mock<IConsensusManager>();
            consensusManager.Setup(c => c.Tip).Returns(provenHeaderChain);

            var behavior = new ProvenHeadersConsensusManagerBehavior(chain, this.initialBlockDownloadState, consensusManager.Object, this.peerBanning, this.extendedLoggerFactory, this.Network, this.chainState, this.checkpoints, this.provenBlockHeaderStore, this.connectionManagerSettings);

            var hashes = new List<uint256>();
            for (int i = 1; i < 5; i++)
            {
                var chainedHeaderToAdd = chain.GetHeader(i);
                hashes.Add(chainedHeaderToAdd.HashBlock);
            }
            hashes.Reverse();

            var blockLocator = new BlockLocator { Blocks = hashes };

            var peerMock = CreatePeerMock();
            behavior.Attach(peerMock.Object);

            var incomingMessage = new IncomingMessage
            {
                Message = new Message(new PayloadProvider().DiscoverPayloads())
                {
                    Magic = this.Network.Magic,
                    Payload = new GetProvenHeadersPayload(blockLocator),
                }
            };

            var provenBlockHeadersToVerifyAgainst = new List<ProvenBlockHeader>();
            for (int i = 5; i <= provenHeaderChain.Height; i++)
            {
                provenBlockHeadersToVerifyAgainst.Add(((PosBlockHeader)provenHeaderChain.GetAncestor(i).Header).ProvenBlockHeader);
            }

            //Trigger the event handler
            peerMock.Object.MessageReceived.ExecuteCallbacksAsync(peerMock.Object, incomingMessage).GetAwaiter().GetResult();

            // Check that the headers we sent is the correct headers.
            var payload = new ProvenHeadersPayload(provenBlockHeadersToVerifyAgainst.ToArray());
            peerMock.Verify(p => p.SendMessageAsync(It.Is<ProvenHeadersPayload>(pl => VerifyHeaders(pl.Headers, provenBlockHeadersToVerifyAgainst)), default(CancellationToken)));
        }

        private bool VerifyHeaders(List<ProvenBlockHeader> fromVerify, List<ProvenBlockHeader> matchWith)
        {
            if (fromVerify.Count != 5)
                return false;

            for (int i = 0; i < 5; i++)
            {
                if (fromVerify[i].GetHash() != matchWith[i].GetHash())
                    return false;
            }

            return true;
        }
    }
}