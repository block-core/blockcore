namespace x42.Features.xServer.Models
{
    public class xServerPeer
    {
        public string Name { get; set; }

        public int NetworkProtocol { get; set; }

        public string NetworkAddress { get; set; }

        public long Priority { get; set; }

        public long NetworkPort { get; set; }

        public string Version { get; set; }

        public long ResponseTime { get; set; }

        public int Tier { get; set; }
    }
}