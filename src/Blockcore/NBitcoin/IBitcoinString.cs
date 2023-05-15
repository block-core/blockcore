using Blockcore.Networks;

namespace Blockcore.NBitcoin
{
    public interface IBitcoinString
    {
        Network Network
        {
            get;
        }
    }
}
