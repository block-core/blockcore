using Blockcore.Networks;

namespace Blockcore.Networks.BCP.Networks
{
    public static class Networks
    {
        public static NetworksSelector BCP
        {
            get
            {
                return new NetworksSelector(() => new BCPMain(), () => new BCPTest(), () => new BCPRegTest());
            }
        }
    }
}
