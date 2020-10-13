using Blockcore.Networks;

namespace NBitcoin
{
    public interface IBitcoinString
    {
        Network Network
        {
            get;
        }
    }
}
