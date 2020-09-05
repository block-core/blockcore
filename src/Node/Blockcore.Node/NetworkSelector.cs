using Blockcore.Configuration;
using NBitcoin.Protocol;

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
                case "BTC":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Bitcoin.Networks.Bitcoin, args: args);
                    break;

                case "CITY":
                    nodeSettings = new NodeSettings(networksSelector: City.Networks.Networks.City, args: args);
                    break;

                case "STRAT":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Stratis.Networks.Stratis, args: args);
                    break;

                case "X42":
                    nodeSettings = new NodeSettings(networksSelector: x42.Networks.Networks.x42, args: args);
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
            }

            return nodeSettings;
        }
    }
}