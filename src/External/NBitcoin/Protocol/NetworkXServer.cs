namespace NBitcoin.Protocol
{
    public class NetworkXServer
    {
        public int NetworkProtocol { get; set; }
        public string NetworkAddress { get; set; }
        public long NetworkPort { get; set; }

        public NetworkXServer(string publicAddress, long port, int networkProtocol = 0)
        {
            this.NetworkProtocol = networkProtocol;
            this.NetworkAddress = publicAddress;
            this.NetworkPort = port;
        }
    }
}