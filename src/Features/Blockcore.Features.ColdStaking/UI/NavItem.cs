using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;
using System.Linq;

namespace Blockcore.Features.Wallet.UI
{
    public class ColdStakingNavigationItem : INavigationItem
    {
        private readonly IWalletManager walletManager;

        public ColdStakingNavigationItem(IWalletManager walletManager)
        {
            this.walletManager = walletManager;
        }

        public string Name => "Cold Staking";
        public string Navigation => "ColdStaking";
        public string Icon => "oi-pulse";
        public bool IsVisible => this.walletManager?.ContainsWallets ?? false;
        public int NavOrder => 30;

    }
}