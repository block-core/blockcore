using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Blockcore.Features.Storage.Models
{
    public class Identity : EntityBase
    {
        [StringLength(512)]
        [JsonProperty(PropertyName = "name")]
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [StringLength(64)]
        [JsonProperty(PropertyName = "shortname")]
        [DataMember(Name = "shortname")]
        public string ShortName { get; set; }

        [StringLength(64)]
        [JsonProperty(PropertyName = "alias")]
        [DataMember(Name = "alias")]
        public string Alias { get; set; }

        [StringLength(255)]
        [JsonProperty(PropertyName = "title")]
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [StringLength(255)]
        [JsonProperty(PropertyName = "email")]
        [DataMember(Name = "email")]
        public string Email { get; set; }

        [StringLength(2000)]
        [JsonProperty(PropertyName = "url")]
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [StringLength(2000)]
        [JsonProperty(PropertyName = "image")]
        [DataMember(Name = "image")]
        public string Image { get; set; }

        /// <summary>
        /// The identity of hubs that this identity use for storage. Number of hubs is currently limited to 3.
        /// </summary>
        [MaxLength(3)]
        [JsonProperty(PropertyName = "hubs")]
        [DataMember(Name = "hubs")]
        public string[] Hubs { get; set; }
    }
}
