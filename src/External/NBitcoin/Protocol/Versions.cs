namespace NBitcoin.Protocol
{
    /// <summary>
    /// Network protocol versioning.
    /// </summary>
    public static class ProtocolVersion
    {
        /// <summary>
        /// Represents th protocol version of POS chains that was used before <see cref="ProtocolVersion.PROVEN_HEADER_VERSION"/>
        /// </summary>
        public const uint POS_PROTOCOL_VERSION = 70000;

        /// <summary>
        /// Initial protocol version; to be increased after version/verack negotiation.
        /// </summary>
        public const uint INIT_PROTO_VERSION = 209;

        /// <summary>
        /// Disconnect from peers older than this protocol version.
        /// </summary>
        public const uint MIN_PEER_PROTO_VERSION = 209;

        /// <summary>
        /// nTime field added to CAddress; starting with this version;
        /// if possible; avoid requesting addresses nodes older than this.
        /// </summary>
        public const uint CADDR_TIME_VERSION = 31402;

        /// <summary>
        /// Only request blocks from nodes outside this range of versions (START).
        /// </summary>
        public const uint NOBLKS_VERSION_START = 32000;

        /// <summary>
        /// Only request blocks from nodes outside this range of versions (END).
        /// </summary>
        public const uint NOBLKS_VERSION_END = 32400;

        /// <summary>
        /// BIP 0031; pong message; is enabled for all versions AFTER this one.
        /// </summary>
        public const uint BIP0031_VERSION = 60000;

        /// <summary>
        /// "mempool" command; enhanced "getdata" behavior starts with this version.
        /// </summary>
        public const uint MEMPOOL_GD_VERSION = 60002;

        /// <summary>
        /// "reject" command.
        /// </summary>
        public const uint REJECT_VERSION = 70002;

        /// <summary>
        /// ! "filter*" commands are disabled without NODE_BLOOM after and including this version.
        /// </summary>
        public const uint NO_BLOOM_VERSION = 70011;

        /// <summary>
        /// ! "sendheaders" command and announcing blocks with headers starts with this version.
        /// </summary>
        public const uint SENDHEADERS_VERSION = 70012;

        /// <summary>
        /// ! Version after which witness support potentially exists.
        /// </summary>
        public const uint WITNESS_VERSION = 70012;

        /// <summary>
        /// Communication between nodes with proven headers is possible after this version.
        /// This is for stratis only. Temporary solution; refers to issue #2144
        /// https://github.com/stratisproject/StratisBitcoinFullNode/issues/2144
        /// </summary>
        public const uint PROVEN_HEADER_VERSION = 70012;

        /// <summary>
        /// shord-id-based block download starts with this version.
        /// </summary>
        public const uint SHORT_IDS_BLOCKS_VERSION = 70014;

        /// <summary>
        /// "feefilter" tells peers to filter invs to you by fee starts with this version.
        /// </summary>
        public const uint FEEFILTER_VERSION = 70013;

        /// <summary>
        /// Oldest supported version of the CirrusNode which this node can connect to.
        /// </summary>
        public const uint CIRRUS_MIN_SUPPORTED_VERSION = 80000;

        /// <summary>
        /// Current version of the CirrusNode.
        /// </summary>
        public const uint CIRRUS_VERSION = 80000;
    }
}