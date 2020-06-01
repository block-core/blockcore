using Blockcore.Interfaces.UI;

namespace Blockcore.Features.Wallet.UI
{
    public class ColdStakingNavigationItem : INavigationItem
    {
        public string Name => "Cold Staking";
        public string Navigation => "ColdStaking";
        public string Icon => "oi-pulse";
    }
}