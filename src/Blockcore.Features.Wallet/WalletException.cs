using System;

namespace Blockcore.Features.Wallet
{
    public class WalletException : Exception
    {
        public WalletException(string message) : base(message)
        {
        }
    }
}
