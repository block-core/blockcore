using Blockcore.Interfaces.UI;
using Blockcore.Features.Wallet.Interfaces;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Networks;

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

    public class ColdStakePoolNavigationItem : INavigationItem
    {
        private readonly Network network;
        private readonly IWalletManager walletManager;
        private readonly NodeSettings nodeSettings;

        public ColdStakePoolNavigationItem(Network network, IWalletManager walletManager, NodeSettings nodeSettings)
        {
            this.network = network;
            this.walletManager = walletManager;
            this.nodeSettings = nodeSettings;
        }

        public string Name => "Cold Staking Pool";
        public string Navigation => "coldstakingpool";
        public string Icon => "oi-calculator";
        public bool IsVisible
        {
            get
            {
                if (!this.network.Consensus.IsProofOfStake)
                    return false;

                if (!this.walletManager?.ContainsWallets ?? false)
                    return false;

                bool enforceStakingFlag = this.nodeSettings.ConfigReader.GetOrDefault("enforceStakingFlag", false);

                if (enforceStakingFlag == false)
                    return false;

                return true;
            }
        }
        
        public int NavOrder => 31;
    }
}