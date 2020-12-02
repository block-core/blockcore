namespace x42.Features.xServer.Models
{
    public class ServerRegisterResult
    {
        public long Id { get; set; }

        public string ProfileName { get; set; }

        public int NetworkProtocol { get; set; }

        public string NetworkAddress { get; set; }

        public long NetworkPort { get; set; }

        public string KeyAddress { get; set; }

        public string SignAddress { get; set; }

        public string FeeAddress { get; set; }

        public string Signature { get; set; }

        public int Tier { get; set; }

        public bool Success { get; set; }

        public string ResultMessage { get; set; }
    }
}