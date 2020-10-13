using System.Threading;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using NBitcoin;

namespace Blockcore.Interfaces
{
    public interface IBlockStoreQueue : IBlockStore
    {
        void ReindexChain(IConsensusManager consensusManager, CancellationToken nodeCancellation);

        /// <summary>Adds a block to the saving queue.</summary>
        /// <param name="chainedHeaderBlock">The block and its chained header pair to be added to pending storage.</param>
        void AddToPending(ChainedHeaderBlock chainedHeaderBlock);

        /// <summary>The highest stored block in the block store cache or <c>null</c> if block store feature is not enabled.</summary>
        ChainedHeader BlockStoreCacheTip { get; }
    }
}
