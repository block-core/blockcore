using System.Linq;
using Blockcore.P2P;
using Blockcore.P2P.Peer;

namespace Blockcore.Utilities.Extensions
{
    public static class NodeConnectionParameterExtensions
    {
        public static PeerAddressManagerBehaviour PeerAddressManagerBehaviour(this NetworkPeerConnectionParameters parameters)
        {
            return parameters.TemplateBehaviors.OfType<PeerAddressManagerBehaviour>().FirstOrDefault();
        }
    }
}