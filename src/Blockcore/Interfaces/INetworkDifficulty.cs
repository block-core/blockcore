using NBitcoin;

namespace Blockcore.Interfaces
{
    public interface INetworkDifficulty
    {
        Target GetNetworkDifficulty();
    }
}
