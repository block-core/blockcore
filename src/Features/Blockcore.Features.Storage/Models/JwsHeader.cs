using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Blockcore.Features.Storage.Models
{
    public class JwsHeader
    {
        [JsonProperty(PropertyName = "typ")]
        [DataMember(Name = "typ")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "kid")]
        [DataMember(Name = "kid")]
        public string KeyID { get; set; }

        [JsonProperty(PropertyName = "alg")]
        [DataMember(Name = "alg")]
        public string Algorithm { get; set; }
    }
}
