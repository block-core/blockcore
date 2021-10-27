namespace Blockcore.Networks.Strax
{
    public static class Networks
    {
        public static NetworksSelector Strax
        {
            get
            {
                return new NetworksSelector(() => new StraxMain(), () => new StraxTest(), () => new StraxRegTest());
            }
        }
    }
}