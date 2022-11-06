using Newtonsoft.Json;

namespace Blockcore.Connection
{
    /// <summary>
    /// Data structure for connected peer node.
    /// </summary>
    public class PeerNodeModel
    {
        /// <summary>
        ///  Peer index.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        /// <summary>
        /// The IP address and port of the peer.
        /// </summary>
        [JsonProperty(PropertyName = "addr")]
        public string Address { get; set; }

        /// <summary>
        /// Local address as reported by the peer.
        /// </summary>
        [JsonProperty(PropertyName = "addrlocal")]
        public string LocalAddress { get; set; }

        /// <summary>
        /// The services offered.
        /// </summary>
        [JsonProperty(PropertyName = "services")]
        public string Services { get; set; }

        /// <summary>
        /// Whether the peer has asked us to relay transactions to it.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "relaytxes")]
        public bool IsRelayTransactions { get; set; }

        ///  <summary>
        ///  The Unix epoch time of the last send from this node.
        /// Currently not populated.
        ///  </summary>
        [JsonProperty(PropertyName = "lastsend")]
        public int LastSend { get; set; }

        ///  <summary>
        ///  The Unix epoch time when we last received data from this node.
        /// Currently not populated.
        ///  </summary>
        [JsonProperty(PropertyName = "lastrecv")]
        public int LastReceive { get; set; }

        ///  <summary>
        ///  The total number of bytes we’ve sent to this node.
        ///  Currently not populated.
        ///  </summary>
        [JsonProperty(PropertyName = "bytessent")]
        public long BytesSent { get; set; }

        ///  <summary>
        ///  The total number of bytes we’ve received from this node.
        ///  Currently not populated.
        ///  </summary>
        [JsonProperty(PropertyName = "bytesrecv")]
        public long BytesReceived { get; set; }

        ///  <summary>
        ///  The connection time in seconds since epoch.
        ///  Currently not populated.
        ///  </summary>
        [JsonProperty(PropertyName = "conntime")]
        public int ConnectionTime { get; set; }

        /// <summary>
        /// The time offset in seconds.
        /// </summary>
        [JsonProperty(PropertyName = "timeoffset")]
        public int TimeOffset { get; set; }

        /// <summary>
        /// The ping time to the node in seconds.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "pingtime")]
        public double PingTime { get; set; }

        /// <summary>
        /// The minimum observed ping time.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "minping")]
        public double MinPing { get; set; }

        /// <summary>
        /// The number of seconds waiting for a ping.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "pingwait")]
        public double PingWait { get; set; }

        /// <summary>
        /// The protocol version number used by this node.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public uint Version { get; set; }

        /// <summary>
        /// The user agent this node sends in its version message.
        /// </summary>
        [JsonProperty(PropertyName = "subver")]
        public string SubVersion { get; set; }

        /// <summary>
        /// Whether node is inbound or outbound connection.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "inbound")]
        public bool Inbound { get; set; }

        /// <summary>
        /// Whether connection was due to addnode.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "addnode")]
        public bool IsAddNode { get; set; }

        /// <summary>
        /// The starting height (block) of the peer.
        /// </summary>
        [JsonProperty(PropertyName = "startingheight")]
        public int StartingHeight { get; set; }

        /// <summary>
        /// The ban score for the node.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "banscore")]
        public int BanScore { get; set; }

        ///  <summary>
        ///  The last header we have in common with this peer.
        ///  Currently not populated.
        ///  </summary>
        [JsonProperty(PropertyName = "synced_headers")]
        public int SynchronizedHeaders { get; set; }

        /// <summary>
        /// The last block we have in common with this peer.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "synced_blocks")]
        public int SynchronizedBlocks { get; set; }

        /// <summary>
        /// Whether the peer is whitelisted.
        /// </summary>
        [JsonProperty(PropertyName = "whitelisted")]
        public bool IsWhiteListed { get; set; }

        /// <summary>
        /// The heights of blocks we're currently asking from this peer.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "inflight")]
        public uint[] Inflight { get; set; }

        /// <summary>
        /// Total sent bytes aggregated by message type.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "bytessent_per_msg")]
        public uint[] BytesSentPerMessage { get; set; }

        /// <summary>
        /// Total received bytes aggregated by message type.
        /// Currently not populated.
        /// </summary>
        [JsonProperty(PropertyName = "bytesrecv_per_msg")]
        public uint[] BytesReceivedPerMessage { get; set; }
    }
}
