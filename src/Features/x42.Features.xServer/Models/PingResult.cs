namespace x42.Features.xServer.Models
{
    /// <summary>
    ///     Class representing the ping result of the currently running node.
    /// </summary>
    public class PingResult
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PingResult" /> class.
        /// </summary>
        public PingResult() { }

        /// <summary>The node's version.</summary>
        public string Version { get; set; }

        /// <summary>The nodes best block height.</summary>
        public ulong BestBlockHeight { get; set; }
    }
}
