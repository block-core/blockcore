using NBitcoin;

namespace Blockcore.Networks.Stratis
{
    public static class Networks
    {
        public static NetworksSelector Stratis
        {
            get
            {
                return new NetworksSelector(() => new StratisMain(), () => new StratisTest(), () => new StratisRegTest());
            }
        }
    }
}