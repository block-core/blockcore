namespace Blockcore.Networks.Xds
{
    public static class Networks
    {
        public static NetworksSelector Xds
        {
            get
            {
                return new NetworksSelector(() => new XdsMain(), () => new XdsTest(), () => new XdsRegTest());
            }
        }
    }
}