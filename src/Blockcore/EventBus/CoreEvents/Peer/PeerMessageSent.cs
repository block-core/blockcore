using System.Net;
using Blockcore.P2P.Protocol;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// A peer message has been sent successfully.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class PeerMessageSent : PeerEventBase
    {
        /// <summary>
        /// Gets the sent message.
        /// </summary>
        public Message Message { get; }

        /// <summary>
        /// Gets the raw size of the message, in bytes.
        /// </summary>
        public int Size { get; }

        public PeerMessageSent(IPEndPoint peerEndPoint, Message message, int size) : base(peerEndPoint)
        {
            this.Message = message;
            this.Size = size;
        }
    }
}