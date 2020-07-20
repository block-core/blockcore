using System.Collections.Generic;

namespace x42.Features.xServer.Models
{
    public class ProfileResult
    {
        public bool Success { get; set; }

        public string ResultMessage { get; set; }

        public string Name { get; set; }

        public string KeyAddress { get; set; }

        public string Signature { get; set; }

        public string PriceLockId { get; set; }

        public List<ProfileField> ProfileFields { get; set; }
    }
}
