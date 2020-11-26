using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Interfaces
{
    /// <summary>
    /// Interface to insert and retrieve <see cref="ProvenBlockHeader"/> items from the database repository.
    /// </summary>
    public interface IProvenBlockHeaderRepository : IProvenBlockHeaderProvider
    {
        /// <summary>
        /// Initializes <see cref="ProvenBlockHeader"/> items database.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Persists <see cref="ProvenBlockHeader"/> items to the database.
        /// </summary>
        /// <param name="provenBlockHeaders">List of <see cref="ProvenBlockHeader"/> items.</param>
        /// <param name="newTip">Block hash and height tip.</param>
        /// <returns><c>true</c> when a <see cref="ProvenBlockHeader"/> is saved to disk, otherwise <c>false</c>.</returns>
        Task PutAsync(SortedDictionary<int, ProvenBlockHeader> provenBlockHeaders, HashHeightPair newTip);
    }
}
