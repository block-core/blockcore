using System.Net;

namespace Blockcore.Features.RPC
{
    public class NodeAddressInfo
    {
        public IPEndPoint Address { get; internal set; }
        public bool Connected { get; internal set; }
    }
}