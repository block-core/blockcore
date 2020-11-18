using Blockcore.Networks;
using NBitcoin;

namespace Rutanio.Networks
{
    public static class Networks
    {
        public static NetworksSelector Rutanio
        {
            get
            {
                return new NetworksSelector(() => new RutanioMain(), () => new RutanioTest(), () => new RutanioRegTest());
            }
        }
    }
}