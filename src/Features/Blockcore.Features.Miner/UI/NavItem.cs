using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;

namespace Blockcore.Features.Wallet.UI
{
    public class StakeNavigationItem : INavigationItem
    {
        private readonly IWalletManager WalletManager;

        public StakeNavigationItem(IWalletManager WalletManager)
        {
            this.WalletManager = WalletManager;
        }

        public string Name => "Staking";
        public string Navigation => "Stake";
        public string Icon => "oi-bolt";
        public bool IsVisible => this.WalletManager?.ContainsWallets ?? false;
        public int NavOrder => 20;
    }
}