using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute;

namespace Blockcore.Features.Storage.Models
{
    public class IdentityDocument : Document<Identity>
    {

    }

    [MessagePackObject]
    public class Identity
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [Key(0)]
        public string Identifier { get; set; }

        /// <summary>
        /// Block hash that was tip when this document was generated. Used to maintain correct sync between nodes.
        /// </summary>
        [StringLength(100)]
        [Key(1)]
        public string Block { get; set; }

        [StringLength(512)]
        [Key(2)]
        public string Name { get; set; }

        [StringLength(64)]
        [Key(3)]
        public string ShortName { get; set; }

        [StringLength(64)]
        [Key(4)]
        public string Alias { get; set; }

        [StringLength(255)]
        [Key(5)]
        public string Title { get; set; }

        [StringLength(255)]
        [Key(6)]
        public string Email { get; set; }

        [StringLength(2000)]
        [Key(7)]
        public string Url { get; set; }

        [StringLength(2000)]
        [Key(8)]
        public string Image { get; set; }
    }
}
