namespace Blockcore.Networks.Molie
{
    public static class Networks
    {
        public static NetworksSelector Molie
        {
            get
            {
                return new NetworksSelector(() => new MolieMain(), () => new MolieTest(),
                    () => new MolieRegTest());
            }
        }
    }
}
