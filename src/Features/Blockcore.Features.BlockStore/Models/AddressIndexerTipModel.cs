using Blockcore.NBitcoin;

namespace Blockcore.Features.BlockStore.Models
{
    public sealed class AddressIndexerTipModel
    {
        public uint256 TipHash { get; set; }
        public int? TipHeight { get; set; }
    }
}