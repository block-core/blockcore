namespace NBitcoin.Protocol
{
    public class NetworkXServer
    {
        public string PublicAddress { get; set; }
        public long Port { get; set; }

        public int NetworkProtocol { get; set; }

        public NetworkXServer(string publicAddress, long port, int networkProtocol = 0)
        {
            this.PublicAddress = publicAddress;
            this.Port = port;
            this.NetworkProtocol = networkProtocol;
        }
    }
}