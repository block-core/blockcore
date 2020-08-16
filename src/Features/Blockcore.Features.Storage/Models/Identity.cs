using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;
using Newtonsoft.Json;
using KeyAttribute = MessagePack.KeyAttribute;

namespace Blockcore.Features.Storage.Models
{
    [MessagePackObject(sortKeys: true)]
    public class Identity : EntityBase
    {
        [StringLength(512)]
        [Key("name")]
        [JsonProperty(PropertyName = "name")]
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [StringLength(64)]
        [Key("shortname")]
        [JsonProperty(PropertyName = "shortname")]
        [DataMember(Name = "shortname")]
        public string ShortName { get; set; }

        [StringLength(64)]
        [Key("alias")]
        [JsonProperty(PropertyName = "alias")]
        [DataMember(Name = "alias")]
        public string Alias { get; set; }

        [StringLength(255)]
        [Key("title")]
        [JsonProperty(PropertyName = "title")]
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [StringLength(255)]
        [Key("email")]
        [JsonProperty(PropertyName = "email")]
        [DataMember(Name = "email")]
        public string Email { get; set; }

        [StringLength(2000)]
        [Key("url")]
        [JsonProperty(PropertyName = "url")]
        [DataMember(Name = "url")]
        public string Url { get; set; }

        [StringLength(2000)]
        [Key("image")]
        [JsonProperty(PropertyName = "image")]
        [DataMember(Name = "image")]
        public string Image { get; set; }

        /// <summary>
        /// The identity of hubs that this identity use for storage. Number of hubs is currently limited to 3.
        /// </summary>
        [MaxLength(3)]
        [Key("hubs")]
        [JsonProperty(PropertyName = "hubs")]
        [DataMember(Name = "hubs")]
        public string[] Hubs { get; set; }
    }
}
