using System.Collections.Generic;

namespace x42.Features.xServer.Models
{
    /// <summary>
    ///     Class representing the top preforming xServers available.
    /// </summary>
    public class TopResult
    {
        /// <summary>The node's version.</summary>
        public List<XServerConnectionInfo> XServers { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TopResult" /> class.
        /// </summary>
        public TopResult()
        {
            this.XServers = new List<XServerConnectionInfo>();
        }
    }

    public class XServerConnectionInfo
    {
        /// <summary>xServer name.</summary>
        public string Name { get; set; }

        /// <summary>xServer connection address.</summary>
        public string Address { get; set; }

        /// <summary>xServer connection port.</summary>
        public long Port { get; set; }

        /// <summary>xServer priority.</summary>
        public long Priotiry { get; set; }
    }
}
