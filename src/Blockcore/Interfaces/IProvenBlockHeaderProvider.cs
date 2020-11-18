using System;
using System.Threading.Tasks;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Interfaces
{
    /// <summary>
    /// Interface <see cref="ProvenBlockHeader"/> provider.
    /// </summary>
    public interface IProvenBlockHeaderProvider : IDisposable
    {
        /// <summary>
        /// Get a <see cref="ProvenBlockHeader"/> corresponding to a block.
        /// </summary>
        /// <param name="blockHeight"> Height used to retrieve the <see cref="ProvenBlockHeader"/>.</param>
        /// <returns><see cref="ProvenBlockHeader"/> retrieved.</returns>
        Task<ProvenBlockHeader> GetAsync(int blockHeight);

        /// <summary>
        /// Height of the block which is currently the tip of the <see cref="ProvenBlockHeader"/>.
        /// </summary>
        HashHeightPair TipHashHeight { get; }
    }
}
