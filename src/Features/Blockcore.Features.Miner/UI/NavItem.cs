using Blockcore.Interfaces.UI;

namespace Blockcore.Features.Wallet.UI
{
    public class StakeNavigationItem : INavigationItem
    {
        public string Name => "Staking";
        public string Navigation => "Stake";
        public string Icon => "oi-bolt";
    }
}