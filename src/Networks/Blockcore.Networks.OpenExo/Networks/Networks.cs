using Blockcore.Networks;
using NBitcoin;

namespace OpenExo.Networks
{
    public static class Networks
    {
        public static NetworksSelector OpenExo
        {
            get
            {
                return new NetworksSelector(() => new OpenExoMain(), () => new OpenExoTest(), () => new OpenExoRegTest());
            }
        }
    }
}
