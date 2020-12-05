using Blockcore.Networks;

namespace Impleum.Networks
{
    public static class Networks
    {
        public static NetworksSelector Impleum => new NetworksSelector(() => new ImpleumMain(), () => new ImpleumTest(), () => new ImpleumRegTest());
    }
}
