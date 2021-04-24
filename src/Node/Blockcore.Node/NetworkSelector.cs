using Blockcore.Configuration;

namespace Blockcore.Node
{
    public static class NetworkSelector
    {
        public static NodeSettings Create(string chain, string[] args)
        {
            chain = chain.ToUpperInvariant();

            NodeSettings nodeSettings = null;

            switch (chain)
            {
                case "BCP":
                    nodeSettings = new NodeSettings(networksSelector: Networks.BCP.Networks.Networks.BCP, args: args);
                    break;

                case "BTC":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Bitcoin.Networks.Bitcoin, args: args);
                    break;

                case "CITY":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.City.Networks.Networks.City, args: args);
                    break;

                case "STRAT":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Stratis.Networks.Stratis, args: args);
                    break;

                case "X42":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.x42.Networks.Networks.x42, args: args);
                    break;

                case "XDS":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Xds.Networks.Xds, args: args);
                    break;

                case "RUTA":
                    nodeSettings = new NodeSettings(networksSelector: Rutanio.Networks.Networks.Rutanio, args: args);
                    break;

                case "EXOS":
                    nodeSettings = new NodeSettings(networksSelector: OpenExo.Networks.Networks.OpenExo, args: args);
                    break;

                case "IMPLX":
                    nodeSettings = new NodeSettings(networksSelector: Networks.Impleum.Networks.Impleum, args: args);
                    break;

                case "X1":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.X1.Networks.X1, args: args);
                    break;

                case "XRC":
                    nodeSettings = new NodeSettings(networksSelector: Networks.XRC.Networks.XRC, args: args);
                    break;

                case "HOME":
                    nodeSettings = new NodeSettings(networksSelector: HomeCoin.Networks.Networks.HomeCoin, args: args);
                    break;

                case "SERF":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.SERF.Networks.SERF, args: args);
                    break;
            }

            return nodeSettings;
        }
    }
}