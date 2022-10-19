using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Networks;

namespace Blockcore.Features.Miner.UI
{
    public class MineNavigationItem : INavigationItem
    {
        private readonly Network network;
        private readonly IWalletManager walletManager;
        public MineNavigationItem(Network network, IWalletManager walletManager)
        {
            this.network = network;
            this.walletManager = walletManager;
        }

        public string Name => "Mining";
        public string Navigation => "mine";
        public string Icon => "oi-pulse";
        public bool IsVisible => CheckIsVisible();
        public int NavOrder => 15;
        private bool CheckIsVisible()
        {
            if (this.network.Consensus.IsProofOfStake && (this.walletManager?.WalletTipHeight > this.network.Consensus.LastPOWBlock))
            {
                return false;
            }
            return this.walletManager?.ContainsWallets ?? false;
        }
    }
    public class StakeNavigationItem : INavigationItem
    {
        private readonly IWalletManager walletManager;

        public StakeNavigationItem(IWalletManager walletManager)
        {
            this.walletManager = walletManager;
        }

        public string Name => "Staking";
        public string Navigation => "Stake";
        public string Icon => "oi-bolt";
        public bool IsVisible => this.walletManager?.ContainsWallets ?? false;
        public int NavOrder => 20;
    }
}