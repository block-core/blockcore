using System.ComponentModel.DataAnnotations;
using MessagePack;
using KeyAttribute = MessagePack.KeyAttribute;

namespace Blockcore.Features.Storage.Models
{
    public class HubDocument : Document<HubData>
    {
        public bool Enabled { get; set; }
    }

    // TODO: Update this HubData type to contain information such as Features that are available on a node. The other values is retrieved from identity instead.
    [MessagePackObject(keyAsPropertyName: true, sortKeys: true)]
    public class HubData
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        [Key(0)]
        public string Identifier { get; set; }

        [Key(1)]
        public string Name { get; set; }

        [Key(2)]
        public string Url { get; set; }

        [Key(3)]
        public string Icon { get; set; }
    }
}
