using System.Collections.Generic;

namespace x42.Features.xServer.Models
{
    public sealed class GetXServerStatsResult
    {
        public int Connected { get; set; }
        public List<xServerPeer> Nodes { get; set; }
    }
}