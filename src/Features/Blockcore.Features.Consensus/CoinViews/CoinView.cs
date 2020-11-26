using System.Collections.Generic;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.Consensus.CoinViews
{

    /// <summary>
    /// Database of UTXOs.
    /// </summary>
    public interface ICoinView
    {
        /// <summary>
        /// Retrieves the block hash of the current tip of the coinview.
        /// </summary>
        /// <returns>Block hash of the current tip of the coinview.</returns>
        HashHeightPair GetTipHash();

        /// <summary>
        /// Persists changes to the coinview (with the tip hash <paramref name="oldBlockHash" />) when a new block
        /// (hash <paramref name="nextBlockHash" />) is added and becomes the new tip of the coinview.
        /// <para>
        /// This method is provided (in <paramref name="unspentOutputs" /> parameter) with information about all
        /// transactions that are either new or were changed in the new block. It is also provided with information
        /// (in <see cref="originalOutputs" />) about the previous state of those transactions (if any),
        /// which is used for <see cref="Rewind" /> operation.
        /// </para>
        /// </summary>
        /// <param name="unspentOutputs">Information about the changes between the old block and the new block. An item in this list represents a list of all outputs
        /// for a specific transaction. If a specific output was spent, the output is <c>null</c>.</param>
        /// <param name="oldBlockHash">Block hash of the current tip of the coinview.</param>
        /// <param name="nextBlockHash">Block hash of the tip of the coinview after the change is applied.</param>
        /// <param name="rewindDataList">List of rewind data items to be persisted.</param>
        void SaveChanges(IList<UnspentOutput> unspentOutputs, HashHeightPair oldBlockHash, HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null);

        /// <summary>
        /// Obtains information about unspent outputs.
        /// </summary>
        /// <param name="utxos">Transaction identifiers for which to retrieve information about unspent outputs.</param>
        /// <returns>
        /// <para>
        /// If an item of <see cref="FetchCoinsResponse.UnspentOutputs"/> is <c>null</c>, it means that outpoint is spent.
        /// </para>
        /// </returns>
        FetchCoinsResponse FetchCoins(OutPoint[] utxos);

        /// <summary>
        /// Check if given utxos are not in cache then pull them from disk and place them in to the cache
        /// </summary>
        /// <param name="utxos">Transaction output identifiers for which to retrieve information about unspent outputs.</param>
        void CacheCoins(OutPoint[] utxos);

        /// <summary>
        /// Rewinds the coinview to the last saved state.
        /// <para>
        /// This operation includes removing the UTXOs of the recent transactions
        /// and restoring recently spent outputs as UTXOs.
        /// </para>
        /// </summary>
        /// <returns>Hash of the block header which is now the tip of the rewound coinview.</returns>
        HashHeightPair Rewind();

        /// <summary>
        /// Gets the rewind data by block height.
        /// </summary>
        /// <param name="height">The height of the block.</param>
        RewindData GetRewindData(int height);
    }
}
