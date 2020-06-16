using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;

namespace Blockcore.Features.Wallet.UI
{
    public class ColdStakingNavigationItem : INavigationItem
    {
        private readonly IWalletManager WalletManager;
        public string Name => "Cold Staking";
        public string Navigation => "ColdStaking";
        public string Icon => "oi-pulse";
        public bool IsVisible => true; //hasWallets();
        public bool hasWallets() {
            var walletManager = this.WalletManager as WalletManager;
            bool hasWallets = false;
            if (walletManager != null)
                {
                    hasWallets = true; //walletManager.ContainsWallets;
                }
            return hasWallets;
        }
    }
}