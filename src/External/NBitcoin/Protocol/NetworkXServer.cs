namespace NBitcoin.Protocol
{
    public class NetworkXServer
    {
        public string PublicAddress { get; set; }
        public long Port { get; set; }

        public bool IsSSL { get; set; }

        public NetworkXServer(string publicAddress, long port, bool isSSL = false)
        {
            this.PublicAddress = publicAddress;
            this.Port = port;
            this.IsSSL = isSSL;
        }
    }
}