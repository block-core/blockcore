using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Blockcore.Connection;
using Blockcore.P2P.Peer;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Blockcore.Utilities.JsonErrors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blockcore.Controllers
{
    public class ConnectionManagerRPCController : FeatureController
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        private readonly IPeerBanning peerBanning;

        public ConnectionManagerRPCController(IConnectionManager connectionManager,
            ILoggerFactory loggerFactory, IPeerBanning peerBanning) : base(connectionManager: connectionManager)
        {
            Guard.NotNull(this.ConnectionManager, nameof(this.ConnectionManager));
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.peerBanning = peerBanning;
        }

        /// <summary>
        /// Sends a command to a node.
        /// </summary>
        /// <param name="endpointStr">The endpoint in string format.</param>
        /// <param name="command">The command to run. Three commands are valid: add, remove, and onetry</param>
        /// <returns><c>true</c> if successful.</returns>
        /// <exception cref="ArgumentException">Thrown if unsupported command given.</exception>
        [ActionName("addnode")]
        [ActionDescription("Adds a node to the connection manager.")]
        public bool AddNode(string endpointStr, string command)
        {
            return AddNode(this.ConnectionManager, this.peerBanning, endpointStr, command);
        }

        public static bool AddNode(IConnectionManager connectionManager, IPeerBanning peerBanning, string endpointStr, string command)
        {
            IPEndPoint endpoint = endpointStr.ToIPEndPoint(connectionManager.Network.DefaultPort);
            switch (command)
            {
                case "add":
                    if (peerBanning.IsBanned(endpoint))
                        throw new InvalidOperationException("Can't perform 'add' for a banned peer.");

                    connectionManager.AddNodeAddress(endpoint);
                    break;

                case "remove":
                    connectionManager.RemoveNodeAddress(endpoint);
                    break;

                case "onetry":
                    if (peerBanning.IsBanned(endpoint))
                        throw new InvalidOperationException("Can't connect to a banned peer.");

                    connectionManager.ConnectAsync(endpoint).GetAwaiter().GetResult();
                    break;

                default:
                    throw new ArgumentException("command");
            }

            return true;
        }

        /// <summary>
        /// RPC implementation of "getpeerinfo".
        /// </summary>
        /// <see cref="https://github.com/bitcoin/bitcoin/blob/0.14/src/rpc/net.cpp"/>
        /// <returns>List of connected peer nodes as <see cref="PeerNodeModel"/>.</returns>
        [ActionName("getpeerinfo")]
        [ActionDescription("Gets peer information from the connection manager.")]
        public List<PeerNodeModel> GetPeerInfo()
        {
            return GetPeerInfo(this.ConnectionManager);
        }

        public static List<PeerNodeModel> GetPeerInfo(IConnectionManager connectionManager)
        {
            var peerList = new List<PeerNodeModel>();

            List<INetworkPeer> peers = connectionManager.ConnectedPeers.ToList();
            foreach (INetworkPeer peer in peers)
            {
                if ((peer != null) && (peer.RemoteSocketAddress != null))
                {
                    var peerNode = new PeerNodeModel
                    {
                        Id = peers.IndexOf(peer),
                        Address = peer.RemoteSocketEndpoint.ToString()
                    };

                    if (peer.PeerVersion != null)
                    {
                        peerNode.LocalAddress = peer.PeerVersion.AddressReceiver?.ToString();
                        peerNode.Services = ((ulong)peer.PeerVersion.Services).ToString("X");
                        peerNode.Version = (uint)peer.PeerVersion.Version;
                        peerNode.SubVersion = peer.PeerVersion.UserAgent;
                        peerNode.StartingHeight = peer.PeerVersion.StartHeight;
                    }

                    var connectionManagerBehavior = peer.Behavior<IConnectionManagerBehavior>();
                    if (connectionManagerBehavior != null)
                    {
                        peerNode.Inbound = peer.Inbound;
                        peerNode.IsWhiteListed = connectionManagerBehavior.Whitelisted;
                    }

                    if (peer.TimeOffset != null)
                    {
                        peerNode.TimeOffset = peer.TimeOffset.Value.Seconds;
                    }

                    peerList.Add(peerNode);
                }
            }

            return peerList;
        }
    }
}