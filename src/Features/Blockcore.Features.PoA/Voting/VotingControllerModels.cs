using System.ComponentModel.DataAnnotations;

namespace Blockcore.Features.PoA.Voting
{
    public class HexPubKeyModel
    {
        [Required(AllowEmptyStrings = false)]
        public string PubKeyHex { get; set; }
    }
}
