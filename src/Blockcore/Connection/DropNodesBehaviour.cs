using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.Chain;
using Blockcore.P2P;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol;
using Blockcore.P2P.Protocol.Behaviors;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Connection
{
    /// <summary>
    /// If the light wallet is only connected to nodes behind
    /// it cannot progress progress to the tip to get the full balance
    /// this behaviour will make sure place is kept for nodes higher then
    /// current tip.
    /// </summary>
    public class DropNodesBehaviour : NetworkPeerBehavior
    {
        /// <summary>Logger factory to create loggers.</summary>
        private readonly ILoggerFactory loggerFactory;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        private readonly ChainIndexer chainIndexer;

        private readonly IConnectionManager connection;

        private readonly decimal dropThreshold;

        public DropNodesBehaviour(ChainIndexer chainIndexer, IConnectionManager connectionManager, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.loggerFactory = loggerFactory;

            this.chainIndexer = chainIndexer;
            this.connection = connectionManager;

            // 80% of current max connections, the last 20% will only
            // connect to nodes ahead of the current best chain.
            this.dropThreshold = 0.8M;
        }

        private Task OnMessageReceivedAsync(INetworkPeer peer, IncomingMessage message)
        {
            if (message.Message.Payload is VersionPayload version)
            {
                IPeerConnector peerConnector = null;
                if (this.connection.ConnectionSettings.Connect.Any())
                    peerConnector = this.connection.PeerConnectors.First(pc => pc is PeerConnectorConnectNode);
                else
                    peerConnector = this.connection.PeerConnectors.First(pc => pc is PeerConnectorDiscovery);

                // Find how much 20% max nodes.
                decimal thresholdCount = Math.Round(peerConnector.MaxOutboundConnections * this.dropThreshold, MidpointRounding.ToEven);

                if (thresholdCount < this.connection.ConnectedPeers.Count())
                {
                    if (version.StartHeight < this.chainIndexer.Height)
                        peer.Disconnect($"Node at height = {version.StartHeight} too far behind current height");
                }
            }

            return Task.CompletedTask;
        }

        protected override void AttachCore()
        {
            this.AttachedPeer.MessageReceived.Register(this.OnMessageReceivedAsync);
        }

        protected override void DetachCore()
        {
            this.AttachedPeer.MessageReceived.Unregister(this.OnMessageReceivedAsync);
        }

        public override object Clone()
        {
            return new DropNodesBehaviour(this.chainIndexer, this.connection, this.loggerFactory);
        }
    }
}