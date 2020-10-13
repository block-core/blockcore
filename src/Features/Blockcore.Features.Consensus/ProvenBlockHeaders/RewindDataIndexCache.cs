using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Networks;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.Consensus.ProvenBlockHeaders
{
    /// <inheritdoc />
    public class RewindDataIndexCache : IRewindDataIndexCache
    {
        private readonly Network network;
        private readonly IFinalizedBlockInfoRepository finalizedBlockInfoRepository;
        private readonly ICheckpoints checkpoints;

        /// <summary>
        /// Internal cache for rewind data index. Key is a TxId + N (N is an index of output in a transaction)
        /// and value is a rewind data index.
        /// </summary>
        private readonly ConcurrentDictionary<OutPoint, int> items;

        /// <summary>
        /// Number of blocks to keep in cache after the flush.
        /// The number of items stored in cache is the sum of inputs used in every transaction in each of those blocks.
        /// </summary>
        private int numberOfBlocksToKeep;

        private int lastCheckpoint;

        /// <summary>
        /// Performance counter to measure performance of the save and get operations.
        /// </summary>
        private readonly BackendPerformanceCounter performanceCounter;

        public RewindDataIndexCache(IDateTimeProvider dateTimeProvider, Network network, IFinalizedBlockInfoRepository finalizedBlockInfoRepository, ICheckpoints checkpoints)
        {
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));

            this.network = network;
            this.finalizedBlockInfoRepository = finalizedBlockInfoRepository;
            this.checkpoints = checkpoints;
            this.items = new ConcurrentDictionary<OutPoint, int>();
            this.lastCheckpoint = this.checkpoints.LastCheckpointHeight;

            this.performanceCounter = new BackendPerformanceCounter(dateTimeProvider);
        }

        /// <inheritdoc />
        public void Initialize(int tipHeight, ICoinView coinView)
        {
            this.items.Clear();

            if (this.lastCheckpoint > tipHeight)
                return;

            HashHeightPair finalBlock = this.finalizedBlockInfoRepository.GetFinalizedBlockInfo();

            this.numberOfBlocksToKeep = (int)this.network.Consensus.MaxReorgLength;

            int heightToSyncTo = tipHeight > this.numberOfBlocksToKeep ? tipHeight - this.numberOfBlocksToKeep : 1;

            if (tipHeight > finalBlock.Height)
            {
                if (heightToSyncTo < finalBlock.Height)
                    heightToSyncTo = finalBlock.Height;

                if (heightToSyncTo < this.lastCheckpoint)
                    heightToSyncTo = this.lastCheckpoint;
            }

            for (int rewindHeight = tipHeight; rewindHeight >= heightToSyncTo; rewindHeight--)
            {
                RewindData rewindData = coinView.GetRewindData(rewindHeight);

                this.AddRewindData(rewindHeight, rewindData);
            }
        }

        /// <summary>
        /// Adding rewind information for a block in to the cache, we only add the unspent outputs.
        /// The cache key is [trxid-outputIndex] and the value is the height of the block on with the rewind data information is kept.
        /// </summary>
        /// <param name="rewindHeight">Height of the rewind data.</param>
        /// <param name="rewindData">The data itself</param>
        private void AddRewindData(int rewindHeight, RewindData rewindData)
        {
            if (rewindData == null)
            {
                throw new ConsensusException($"Rewind data of height '{rewindHeight}' was not found!");
            }

            if (rewindData.OutputsToRestore == null || rewindData.OutputsToRestore.Count == 0)
            {
                return;
            }

            foreach (RewindDataOutput unspent in rewindData.OutputsToRestore)
            {
                this.items[unspent.OutPoint] = rewindHeight;
            }
        }

        /// <inheritdoc />
        public void Remove(int tipHeight, ICoinView coinView)
        {
            if (this.lastCheckpoint > tipHeight)
                return;

            this.SaveAndEvict(tipHeight, null);

            int bottomHeight = tipHeight > this.numberOfBlocksToKeep ? tipHeight - this.numberOfBlocksToKeep : 1;

            RewindData rewindData = coinView.GetRewindData(bottomHeight);
            this.AddRewindData(bottomHeight, rewindData);
        }

        /// <inheritdoc />
        public void SaveAndEvict(int tipHeight, Dictionary<OutPoint, int> indexData)
        {
            if (this.lastCheckpoint > tipHeight)
                return;

            if (indexData != null)
            {
                using (new StopwatchDisposable(o => this.performanceCounter.AddInsertTime(o)))
                {
                    foreach (KeyValuePair<OutPoint, int> indexRecord in indexData)
                    {
                        this.items[indexRecord.Key] = indexRecord.Value;
                    }
                }
            }

            int heightToKeepItemsTo = tipHeight > this.numberOfBlocksToKeep ? tipHeight - this.numberOfBlocksToKeep : 1; ;

            List<KeyValuePair<OutPoint, int>> listOfItems = this.items.ToList();
            foreach (KeyValuePair<OutPoint, int> item in listOfItems)
            {
                if ((item.Value < heightToKeepItemsTo) || (item.Value > tipHeight))
                {
                    this.items.TryRemove(item.Key, out int unused);
                }
            }
        }

        /// <inheritdoc />
        public int? Get(uint256 transactionId, int transactionOutputIndex)
        {
            var key = new OutPoint(transactionId, transactionOutputIndex);

            if (this.items.TryGetValue(key, out int rewindDataIndex))
                return rewindDataIndex;

            return null;
        }
    }
}