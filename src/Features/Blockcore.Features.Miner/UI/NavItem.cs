using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Networks;

namespace Blockcore.Features.Miner.UI
{
    public class MineNavigationItem : INavigationItem
    {
        private readonly Network network;
        private readonly IWalletManager WalletManager;
        public MineNavigationItem(Network network, IWalletManager WalletManager)
        {
            this.network = network;
            this.WalletManager = WalletManager;
        }

        public string Name => "Mining";
        public string Navigation => "mine";
        public string Icon => "oi-pulse";
        public bool IsVisible => CheckIsVisible();
        public int NavOrder => 15;
        private bool CheckIsVisible()
        {
            if (this.network.Consensus.IsProofOfStake && (this.WalletManager?.WalletTipHeight > this.network.Consensus.LastPOWBlock))
            {
                return false;
            }
            return this.WalletManager?.ContainsWallets ?? false;
        }
    }
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