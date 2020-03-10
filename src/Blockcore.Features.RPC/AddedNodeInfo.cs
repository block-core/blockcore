using System.Collections.Generic;
using System.Net;

namespace Blockcore.Features.RPC
{
    public class AddedNodeInfo
    {
        public EndPoint AddedNode { get; internal set; }
        public bool Connected { get; internal set; }
        public IEnumerable<NodeAddressInfo> Addresses { get; internal set; }
    }
}