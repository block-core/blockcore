﻿using System.Collections.Generic;
using System.Threading;
using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus.CoinViews
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
        /// <param name="rewindDataList">List of rewind data items to be persisted. This should only be used when calling <see cref="DBreezeCoinView.SaveChanges" />.</param>
        void SaveChanges(IList<UnspentOutput> unspentOutputs, HashHeightPair oldBlockHash, HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null);

        /// <summary>
        /// Obtains information about unspent outputs for specific transactions and also retrieves information about the coinview's tip.
        /// </summary>
        /// <param name="utxos">Transaction identifiers for which to retrieve information about unspent outputs.</param>
        /// <returns>
        /// Coinview tip's hash and information about unspent outputs in the requested transactions.
        /// <para>
        /// i-th item in <see cref="FetchCoinsResponse.UnspentOutputs"/> array is the information of the unspent outputs for i-th transaction in <paramref name="txIds"/>.
        /// If the i-th item of <see cref="FetchCoinsResponse.UnspentOutputs"/> is <c>null</c>, it means that there are no unspent outputs in the given transaction.
        /// </para>
        /// </returns>
        FetchCoinsResponse FetchCoins(OutPoint[] utxos);

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
