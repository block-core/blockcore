using System;

namespace Blockcore.NBitcoin.Protocol
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string message)
            : base(message)
        {
        }
    }
}