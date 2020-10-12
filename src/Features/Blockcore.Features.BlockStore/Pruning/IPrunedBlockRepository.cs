using Blockcore.Consensus.Chain;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Pruning
{
    /// <summary>
    /// Prunes and compacts the block store database by deleting blocks lower than a certain height and recreating the database file on disk.
    /// </summary>
    public interface IPrunedBlockRepository
    {
        /// <summary>
        /// Initializes the pruned block repository.
        /// </summary>
        void Initialize();

        /// <summary>
        /// The lowest block hash and height that the repository has.
        /// <para>
        /// This also indicated where the node has been pruned up to.
        /// </para>
        /// </summary>
        HashHeightPair PrunedTip { get; }

        /// <summary>
        /// Sets the pruned tip.
        /// <para>
        /// It will be saved once the block database has been compacted on node initialization or shutdown.
        /// </para>
        /// </summary>
        /// <param name="tip">The tip to set.</param>
        void UpdatePrunedTip(ChainedHeader tip);

        /// <summary>
        /// Prepare the pruned tip.
        /// </summary>
        void PrepareDatabase();
    }
}