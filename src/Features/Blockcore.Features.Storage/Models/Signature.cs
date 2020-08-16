using System;
using System.Collections.Generic;
using System.Text;

namespace Blockcore.Features.Storage.Models
{
    public class Signature
    {
        //[JsonProperty(PropertyName = "type")]
        //[DataMember(Name = "type")]
        public string Type { get; set; } = "sha256-ecdsa-secp256k1-v1";

        //[JsonProperty(PropertyName = "value")]
        //[DataMember(Name = "value")]
        public string Identity { get; set; }

        //[JsonProperty(PropertyName = "value")]
        //[DataMember(Name = "value")]
        public string Value { get; set; }
    }
}
