using Blockcore.Interfaces.UI;

namespace Blockcore.Features.Wallet.UI
{
    public class WalletNavigationItem : INavigationItem
    {
        public string Name => "Wallets";
        public string Navigation => "Wallets";
        public string Icon => "oi-folder";
        public bool IsVisible => true;
        public int NavOrder => 10;
    }
}