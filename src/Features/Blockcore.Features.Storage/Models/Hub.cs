using System.ComponentModel.DataAnnotations;

namespace Blockcore.Features.Storage.Models
{
    public class HubDocument : Document<HubData>
    {
        public bool Enabled { get; set; }
    }

    // TODO: Update this HubData type to contain information such as Features that are available on a node. The other values is retrieved from identity instead.
    public class HubData
    {
        [StringLength(255, MinimumLength = 1)]
        [Required]
        public string Identifier { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Icon { get; set; }
    }
}
