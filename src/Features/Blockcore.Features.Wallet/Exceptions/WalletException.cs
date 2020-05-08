using System;

namespace Blockcore.Features.Wallet.Exceptions
{
    public class WalletException : Exception
    {
        public WalletException(string message) : base(message)
        {
        }
    }
}
