using Blockcore.Networks;

namespace Blockcore.NBitcoin
{
    public interface IBech32Data : IBitcoinString
    {
        Bech32Type Type
        {
            get;
        }
    }
}
