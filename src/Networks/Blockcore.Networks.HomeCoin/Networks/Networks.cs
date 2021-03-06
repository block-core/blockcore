using Blockcore.Networks;

namespace HomeCoin.Networks
{
    public static class Networks
    {
        public static NetworksSelector HomeCoin
        {
            get
            {
                return new NetworksSelector(() => new HomeCoinMain(), () => new HomeCoinTest(), () => new HomeCoinRegTest());
            }
        }
    }
}
