using System.Net;
using Blockcore.P2P.Protocol;

namespace Blockcore.EventBus.CoreEvents.Peer
{
    /// <summary>
    /// A peer message failed to be sent.
    /// </summary>
    /// <seealso cref="EventBase" />
    public class PeerMessageSendFailure : PeerEventBase
    {
        /// <summary>
        /// The failed message. Can be null if the exception was caused during the Message creation.
        /// </value>
        public Message Message { get; }

        public System.Exception Exception { get; }

        public PeerMessageSendFailure(IPEndPoint peerEndPoint, Message message, System.Exception exception) : base(peerEndPoint)
        {
            this.Message = message;
            this.Exception = exception;
        }
    }
}