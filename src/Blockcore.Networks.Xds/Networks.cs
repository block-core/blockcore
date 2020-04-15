using NBitcoin;

namespace Blockcore.Networks.Xds
{
    public static class Networks
    {
        public static NetworksSelector Xds
        {
            get
            {
                return new NetworksSelector(() => new XdsMain(), () => null, () => null);
            }
        }
    }
}