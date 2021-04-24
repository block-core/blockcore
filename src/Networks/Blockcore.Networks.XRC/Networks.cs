using NBitcoin;

namespace Blockcore.Networks.XRC
{
    public static class Networks
    {
        public static NetworksSelector XRC
        {
            get
            {
                return new NetworksSelector(() => new XRCMain(), () => new XRCTest(), () => new XRCRegTest());
            }
        }
    }
}