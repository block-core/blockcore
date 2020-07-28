using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;
using System.Linq;

namespace Blockcore.Features.Wallet.UI
{
    public class ColdStakingNavigationItem : INavigationItem
    {
        private readonly IWalletManager WalletManager;

        public ColdStakingNavigationItem(IWalletManager WalletManager)
        {
            this.WalletManager = WalletManager;
        }

        public string Name => "Cold Staking";
        public string Navigation => "ColdStaking";
        public string Icon => "oi-pulse";
        public bool IsVisible => this.WalletManager?.ContainsWallets ?? false;
        public int NavOrder => 30;

    }
}