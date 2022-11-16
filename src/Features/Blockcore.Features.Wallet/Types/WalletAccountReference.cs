using System;
using Blockcore.Utilities;

namespace Blockcore.Features.Wallet.Types
{
    public class WalletAccountReference
    {
        public WalletAccountReference()
        {
        }

        public WalletAccountReference(string walletName, string accountName)
        {
            Guard.NotEmpty(walletName, nameof(walletName));
            Guard.NotEmpty(accountName, nameof(accountName));

            this.WalletName = walletName;
            this.AccountName = accountName;
        }

        public string WalletName { get; set; }

        public string AccountName { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as WalletAccountReference;
            if (item == null)
                return false;
            return GetId().Equals(item.GetId());
        }

        public static bool operator ==(WalletAccountReference a, WalletAccountReference b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if ((a is null) || (b is null))
                return false;
            return a.GetId().Equals(b.GetId());
        }

        public static bool operator !=(WalletAccountReference a, WalletAccountReference b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return GetId().GetHashCode();
        }

        internal Tuple<string, string> GetId()
        {
            return Tuple.Create(this.WalletName, this.AccountName);
        }

        public override string ToString()
        {
            return $"{this.WalletName}:{this.AccountName}";
        }
    }
}
