using System.Collections.Generic;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Interfaces;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Repository
{
    /// <summary>
    /// <see cref="IBlockRepository"/> is the interface to all the logics interacting with the blocks stored in the database.
    /// </summary>
    public interface IBlockRepository : IBlockStore
    {
        /// <summary> The dbreeze database engine.</summary>
        object DbInstance { get; }

        /// <summary>Really ugly temporary hack.</summary>
        object Locker { get; }

        /// <summary>
        /// Deletes blocks and indexes for transactions that belong to deleted blocks.
        /// <para>
        /// It should be noted that this does not delete the entries from disk (only the references are removed) and
        /// as such the file size remains the same.
        /// </para>
        /// </summary>
        /// <remarks>TODO: This will need to be revisited once DBreeze has been fixed or replaced with a solution that works.</remarks>
        /// <param name="hashes">List of block hashes to be deleted.</param>
        void DeleteBlocks(List<uint256> hashes);

        /// <summary>
        /// Persist the next block hash and insert new blocks into the database.
        /// </summary>
        /// <param name="newTip">Hash and height of the new repository's tip.</param>
        /// <param name="blocks">Blocks to be inserted.</param>
        void PutBlocks(HashHeightPair newTip, List<Block> blocks);

        /// <summary>
        /// Wipe out blocks and their transactions then replace with a new block.
        /// </summary>
        /// <param name="newTip">Hash and height of the new repository's tip.</param>
        /// <param name="hashes">List of all block hashes to be deleted.</param>
        void Delete(HashHeightPair newTip, List<uint256> hashes);

        /// <summary>
        /// Determine if a block already exists
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns><c>true</c> if the block hash can be found in the database, otherwise return <c>false</c>.</returns>
        bool Exist(uint256 hash);

        /// <summary>
        /// Iterate over every block in the database.
        /// If <see cref="TxIndex"/> is true, we store the block hash alongside the transaction hash in the transaction table, otherwise clear the transaction table.
        /// </summary>
        void ReIndex();

        /// <summary>
        /// Set whether to index transactions by block hash, as well as storing them inside of the block.
        /// </summary>
        /// <param name="txIndex">Whether to index transactions.</param>
        void SetTxIndex(bool txIndex);

        /// <summary>Hash and height of the repository's tip.</summary>
        HashHeightPair TipHashAndHeight { get; }

        /// <summary> Indicates that the node should store all transaction data in the database.</summary>
        bool TxIndex { get; }

        /// <summary>
        /// Enumerate the list of blocks from block store.
        /// </summary>
        IEnumerable<Block> EnumerateBatch(List<ChainedHeader> headers);
    }
}