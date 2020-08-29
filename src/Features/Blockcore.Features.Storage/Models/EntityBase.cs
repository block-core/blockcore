using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Blockcore.Features.Storage.Models
{
    public abstract class EntityBase
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [JsonProperty(PropertyName = "identifier")]
        [DataMember(Name = "identifier")]
        public string Identifier { get; set; }

        [StringLength(255, MinimumLength = 1)]
        [Required]
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
        [JsonProperty(PropertyName = "@state")]
        [DataMember(Name = "@state")]
        public ushort State { get; set; }

        /// <summary>
        /// UNIX epoch time when the document was signed. Used to maintain correct sync between nodes.
        /// </summary>
        [Required]
        [JsonProperty(PropertyName = "iat")] // Rely on "Issued At" standard claim to set dates.
        [DataMember(Name = "iat")]
        public long Timestamp { get; set; }
    }
}
