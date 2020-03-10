﻿using System.Net;
using Blockcore.P2P.Protocol;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// A peer message has been received and parsed
    /// </summary>
    /// <seealso cref="Stratis.Bitcoin.EventBus.EventBase" />
    public class PeerMessageReceived : PeerEventBase
    {
        public Message Message { get; }

        public int MessageSize { get; }

        public PeerMessageReceived(IPEndPoint peerEndPoint, Message message, int messageSize) : base(peerEndPoint)
        {
            this.Message = message;
            this.MessageSize = messageSize;
        }
    }
}