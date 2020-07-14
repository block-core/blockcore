using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using LiteDB;

namespace Blockcore.Features.Storage.Models
{
    public class DataEntity
    {
        [BsonId]
        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string Id { get; set; }

        public string Data { get; set; }
    }
}
