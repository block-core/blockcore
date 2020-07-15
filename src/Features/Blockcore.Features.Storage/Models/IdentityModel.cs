using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute;

namespace Blockcore.Features.Storage.Models
{
    [MessagePackObject]
    public class IdentityModel
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [Key(0)]
        public string Id { get; set; }

        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public string ShortName { get; set; }

        [Key(3)]
        public string Alias { get; set; }

        [Key(4)]
        public string Title { get; set; }

        [Key(5)]
        public string Email { get; set; }

        // public DateTime Created { get; set; }

        // public DateTime Updated { get; set; }
    }
}
