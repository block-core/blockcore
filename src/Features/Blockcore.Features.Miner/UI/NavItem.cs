using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;

namespace Blockcore.Features.Wallet.UI
{
    public class StakeNavigationItem : INavigationItem
    {
         public string Name => "Staking";
        public string Navigation => "Stake";
        public string Icon => "oi-bolt";     
        private readonly IWalletManager walletManager;
        public bool IsVisible => true; //hasWallets();
        public bool hasWallets() {
            var walletManager = this.walletManager as WalletManager;
            bool hasWallets = false;
            if (walletManager != null)
                {
                    hasWallets = true; //walletManager.ContainsWallets;
                }
            return hasWallets;
        }
    }
}