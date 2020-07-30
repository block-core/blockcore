using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute;

namespace Blockcore.Features.Storage.Models
{
    public class IdentityDocument : Document<Identity>
    {
        /// <summary>
        /// Version of identity that this document holds. This is not revisions of the document instance, but version of type definition used for compatibility.
        /// </summary>
        public short Version { get; set; }
    }

    [MessagePackObject]
    public class Identity
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [Key(0)]
        public string Identifier { get; set; }

        /// <summary>
        /// Block height when this document was generated. Used to maintain correct sync between nodes.
        /// </summary>
        [Key(1)]
        public int Height { get; set; }

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

        /// <summary>
        /// The identity of hubs that this identity use for storage. Number of hubs is currently limited to 5.
        /// </summary>
        [MaxLength(5)]
        [Key(9)]
        public string[] Hubs { get; set; }
    }
}
