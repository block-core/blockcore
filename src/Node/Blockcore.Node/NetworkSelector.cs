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
                    nodeSettings = new NodeSettings(networksSelector: City.Networks.Networks.City,
                        protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                    {
                        MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                    };
                    break;
                case "STRAT":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Stratis.Networks.Stratis,
                        protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args);
                    break;
                case "X42":
                    nodeSettings = new NodeSettings(networksSelector: x42.Networks.Networks.x42,
                        protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                    {
                        MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                    };
                    break;
                case "XDS":
                    nodeSettings = new NodeSettings(networksSelector: Blockcore.Networks.Xds.Networks.Xds,
                        protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                    {
                        MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                    };
                    break;
            }

            return nodeSettings;
        }
    }
}
