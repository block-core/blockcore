using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Blockcore.Features.Storage.Models
{
    /// <summary>
    /// Index 0 to 9 is reserved for usage of the storage feature. All types should start at index 10 for custom fields.
    /// </summary>
    [MessagePackObject(sortKeys: true)]
    public abstract class EntityBase
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [Key("identifier")]
        [JsonProperty(PropertyName = "identifier")]
        [DataMember(Name = "identifier")]
        public string Identifier { get; set; }

        /// <summary>
        /// Block height when this document was generated. Used to maintain correct sync between nodes.
        /// </summary>
        [Key("height")]
        [JsonProperty(PropertyName = "height")]
        [DataMember(Name = "height")]
        public int Height { get; set; }

        [StringLength(255, MinimumLength = 1)]
        [Required]
        [Key("@type")]
        [JsonProperty(PropertyName = "@type")] // Follow the JSON-LD standard: https://json-ld.org/.
        [DataMember(Name = "@type")]
        public string Type { get; set; }

        /// <summary>
        /// State is used to indicate status of an entity and used when a document is marked for delete.
        /// Can be anything between 0 and 65,535 (no negative values).
        /// Current known states: 
        /// 0: Normal and active documents.
        /// 999: Deleted.
        /// </summary>
        [Required]
        [Key("@state")]
        [JsonProperty(PropertyName = "@state")]
        [DataMember(Name = "@state")]
        public ushort State { get; set; }
    }
}
