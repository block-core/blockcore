using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Base;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.CoinViews
{
    /// <summary>
    /// Cache layer for coinview prevents too frequent updates of the data in the underlying storage.
    /// </summary>
    public class CachedCoinView : ICoinView
    {
        /// <summary>
        /// Item of the coinview cache that holds information about the unspent outputs
        /// as well as the status of the item in relation to the underlying storage.
        /// </summary>
        private class CacheItem
        {
            public OutPoint OutPoint;

            /// <summary>Information about transaction's outputs. Spent outputs are nulled.</summary>
            public Coins Coins;

            /// <summary><c>true</c> if the unspent output information is stored in the underlying storage, <c>false</c> otherwise.</summary>
            public bool ExistInInner;

            /// <summary><c>true</c> if the information in the cache is different than the information in the underlying storage.</summary>
            public bool IsDirty;

            public long GetSize
            {
                get
                {
                    // The fixed output size plus script size if present
                    return 32 + 4 + (this.Coins?.TxOut.ScriptPubKey.Length ?? 0);
                }
            }

            public long GetScriptSize
            {
                get
                {
                    // Script size if present
                    return this.Coins?.TxOut.ScriptPubKey.Length ?? 0;
                }
            }
        }

        /// <summary>
        /// Length of the coinview cache flushing interval in seconds, in case of a crash up to that number of seconds of syncing blocks are lost.
        /// </summary>
        /// <remarks>
        /// The longer the time interval the better performant the coinview will be,
        /// UTXOs that are added and deleted before they are flushed never reach the underline disk
        /// this saves 3 operations to disk (write the coinview and later read and delete it).
        /// However if this interval is too high the cache will be filled with dirty items
        /// Also a crash will mean a big redownload of the chain.
        /// </remarks>
        /// <seealso cref="lastCacheFlushTime"/>
        public int CacheFlushTimeIntervalSeconds { get; set; }

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Maximum number of transactions in the cache.</summary>
        public int MaxCacheSizeBytes { get; set; }

        /// <summary>Statistics of hits and misses in the cache.</summary>
        private CachePerformanceCounter performanceCounter { get; set; }

        /// <summary>Lock object to protect access to <see cref="cachedUtxoItems"/>, <see cref="blockHash"/>, <see cref="cachedRewindData"/>, and <see cref="innerBlockHash"/>.</summary>
        private readonly object lockobj;

        /// <summary>Hash of the block headers of the tip of the coinview.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private HashHeightPair blockHash;

        /// <summary>Hash of the block headers of the tip of the underlaying coinview.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private HashHeightPair innerBlockHash;

        /// <summary>Coin view at one layer below this implementaiton.</summary>
        private readonly ICoindb coindb;

        /// <summary>Pending list of rewind data to be persisted to a persistent storage.</summary>
        /// <remarks>All access to this list has to be protected by <see cref="lockobj"/>.</remarks>
        private readonly Dictionary<int, RewindData> cachedRewindData;

        /// <inheritdoc />
        public ICoindb ICoindb => this.coindb;

        /// <summary>Storage of POS block information.</summary>
        private readonly StakeChainStore stakeChainStore;

        /// <summary>
        /// The rewind data index store.
        /// </summary>
        private readonly IRewindDataIndexCache rewindDataIndexCache;

        /// <summary>Information about cached items mapped by transaction IDs the cached item's unspent outputs belong to.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private readonly Dictionary<OutPoint, CacheItem> cachedUtxoItems;

        /// <summary>Number of items in the cache.</summary>
        /// <remarks>The getter violates the lock contract on <see cref="cachedUtxoItems"/>, but the lock here is unnecessary as the <see cref="cachedUtxoItems"/> is marked as readonly.</remarks>
        private int cacheCount => this.cachedUtxoItems.Count;

        /// <summary>Number of items in the rewind data.</summary>
        /// <remarks>The getter violates the lock contract on <see cref="cachedRewindData"/>, but the lock here is unnecessary as the <see cref="cachedRewindData"/> is marked as readonly.</remarks>
        private int rewindDataCount => this.cachedRewindData.Count;

        private long dirtyCacheCount;
        private long cacheSizeBytes;
        private long rewindDataSizeBytes;
        private DateTime lastCacheFlushTime;
        private readonly Network network;
        private readonly ICheckpoints checkpoints;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ConsensusSettings consensusSettings;
        private CachePerformanceSnapshot latestPerformanceSnapShot;
        private int lastCheckpointHeight;

        private readonly Random random;

        public CachedCoinView(
            Network network,
            ICheckpoints checkpoints,
            ICoindb coindb,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory,
            INodeStats nodeStats,
            ConsensusSettings consensusSettings,
            StakeChainStore stakeChainStore = null,
            IRewindDataIndexCache rewindDataIndexCache = null)
        {
            Guard.NotNull(coindb, nameof(CachedCoinView.coindb));

            this.coindb = coindb;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.network = network;
            this.checkpoints = checkpoints;
            this.dateTimeProvider = dateTimeProvider;
            this.consensusSettings = consensusSettings;
            this.stakeChainStore = stakeChainStore;
            this.rewindDataIndexCache = rewindDataIndexCache;
            this.lockobj = new object();
            this.cachedUtxoItems = new Dictionary<OutPoint, CacheItem>();
            this.performanceCounter = new CachePerformanceCounter(this.dateTimeProvider);
            this.lastCacheFlushTime = this.dateTimeProvider.GetUtcNow();
            this.cachedRewindData = new Dictionary<int, RewindData>();
            this.random = new Random();

            this.lastCheckpointHeight = this.checkpoints.LastCheckpointHeight;

            this.MaxCacheSizeBytes = consensusSettings.MaxCoindbCacheInMB * 1024 * 1024;
            this.CacheFlushTimeIntervalSeconds = consensusSettings.CoindbIbdFlushMin * 60;

            nodeStats.RegisterStats(this.AddBenchStats, StatsType.Benchmark, this.GetType().Name, 300);
        }

        public HashHeightPair GetTipHash()
        {
            if (this.blockHash == null)
            {
                HashHeightPair response = this.coindb.GetTipHash();

                this.innerBlockHash = response;
                this.blockHash = this.innerBlockHash;
            }

            return this.blockHash;
        }

        /// <inheritdoc />
        public void CacheCoins(OutPoint[] utxos)
        {
            lock (this.lockobj)
            {
                var missedOutpoint = new List<OutPoint>();
                foreach (OutPoint outPoint in utxos)
                {
                    if (!this.cachedUtxoItems.TryGetValue(outPoint, out CacheItem cache))
                    {
                        this.logger.LogDebug("Prefetch Utxo '{0}' not found in cache.", outPoint);
                        missedOutpoint.Add(outPoint);
                    }
                }

                this.performanceCounter.AddCacheMissCount(missedOutpoint.Count);
                this.performanceCounter.AddCacheHitCount(utxos.Length - missedOutpoint.Count);

                if (missedOutpoint.Count > 0)
                {
                    FetchCoinsResponse fetchedCoins = this.coindb.FetchCoins(missedOutpoint.ToArray());
                    foreach (var unspentOutput in fetchedCoins.UnspentOutputs)
                    {
                        var cache = new CacheItem()
                        {
                            ExistInInner = unspentOutput.Value.Coins != null,
                            IsDirty = false,
                            OutPoint = unspentOutput.Key,
                            Coins = unspentOutput.Value.Coins
                        };
                        this.logger.LogDebug("Prefetch CacheItem added to the cache, UTXO: '{0}', Coin:'{1}'.", cache.OutPoint, cache.Coins);
                        this.cachedUtxoItems.Add(cache.OutPoint, cache);
                        this.cacheSizeBytes += cache.GetSize;
                    }
                }
            }
        }

        /// <inheritdoc />
        public FetchCoinsResponse FetchCoins(OutPoint[] utxos)
        {
            Guard.NotNull(utxos, nameof(utxos));

            var result = new FetchCoinsResponse();
            var missedOutpoint = new List<OutPoint>();

            lock (this.lockobj)
            {
                foreach (OutPoint outPoint in utxos)
                {
                    if (!this.cachedUtxoItems.TryGetValue(outPoint, out CacheItem cache))
                    {
                        this.logger.LogDebug("Utxo '{0}' not found in cache.", outPoint);
                        missedOutpoint.Add(outPoint);
                    }
                    else
                    {
                        this.logger.LogDebug("Utxo '{0}' found in cache, UTXOs:'{1}'.", outPoint, cache.Coins);
                        result.UnspentOutputs.Add(outPoint, new UnspentOutput(outPoint, cache.Coins));
                    }
                }

                this.performanceCounter.AddMissCount(missedOutpoint.Count);
                this.performanceCounter.AddHitCount(utxos.Length - missedOutpoint.Count);

                if (missedOutpoint.Count > 0)
                {
                    this.logger.LogDebug("{0} cache missed transaction needs to be loaded from underlying CoinView.", missedOutpoint.Count);
                    FetchCoinsResponse fetchedCoins = this.coindb.FetchCoins(missedOutpoint.ToArray());

                    foreach (var unspentOutput in fetchedCoins.UnspentOutputs)
                    {
                        result.UnspentOutputs.Add(unspentOutput.Key, unspentOutput.Value);

                        var cache = new CacheItem()
                        {
                            ExistInInner = unspentOutput.Value.Coins != null,
                            IsDirty = false,
                            OutPoint = unspentOutput.Key,
                            Coins = unspentOutput.Value.Coins
                        };

                        this.logger.LogDebug("CacheItem added to the cache, UTXO '{0}', Coin:'{1}'.", cache.OutPoint, cache.Coins);
                        this.cachedUtxoItems.Add(cache.OutPoint, cache);
                        this.cacheSizeBytes += cache.GetSize;
                    }
                }

                // Check if we need to evict items form the cache.
                // This happens every time data is fetched fomr coindb

                this.TryEvictCacheLocked();
            }

            return result;
        }

        /// <summary>
        /// Deletes some items from the cache to free space for new items.
        /// Only items that are persisted in the underlaying storage can be deleted from the cache.
        /// </summary>
        /// <remarks>Should be protected by <see cref="lockobj"/>.</remarks>
        private void TryEvictCacheLocked()
        {
            // Calculate total size of cache.
            long totalBytes = this.cacheSizeBytes + this.rewindDataSizeBytes;

            if (totalBytes > this.MaxCacheSizeBytes)
            {
                this.logger.LogDebug("Cache is full now with {0} bytes, evicting.", totalBytes);

                List<CacheItem> itemsToRemove = new List<CacheItem>();
                foreach (KeyValuePair<OutPoint, CacheItem> entry in this.cachedUtxoItems)
                {
                    if (!entry.Value.IsDirty && entry.Value.ExistInInner)
                    {
                        if ((this.random.Next() % 3) == 0)
                        {
                            itemsToRemove.Add(entry.Value);
                        }
                    }
                }

                foreach (CacheItem item in itemsToRemove)
                {
                    this.logger.LogDebug("Transaction Id '{0}' selected to be removed from the cache, CacheItem:'{1}'.", item.OutPoint, item.Coins);
                    this.cachedUtxoItems.Remove(item.OutPoint);
                    this.cacheSizeBytes -= item.GetSize;
                    if (item.IsDirty) this.dirtyCacheCount--;
                }
            }
        }

        /// <summary>
        /// Check if periodic flush is required.
        /// The conditions to flash the cache are if <see cref="CacheFlushTimeIntervalSeconds"/> is elapsed
        /// or if <see cref="MaxCacheSizeBytes"/> is reached.
        /// </summary>
        /// <returns>True if the coinview needs to flush</returns>
        public bool ShouldFlush()
        {
            DateTime now = this.dateTimeProvider.GetUtcNow();
            bool flushTimeLimit = (now - this.lastCacheFlushTime).TotalSeconds >= this.CacheFlushTimeIntervalSeconds;

            // The size of the cache was reached and most likely TryEvictCacheLocked didn't work
            // so the cache is polluted with flushable items, then we flush anyway.

            long totalBytes = this.cacheSizeBytes + this.rewindDataSizeBytes;
            bool flushSizeLimit = totalBytes > this.MaxCacheSizeBytes;

            if (!flushTimeLimit && !flushSizeLimit)
            {
                return false;
            }

            this.logger.LogDebug("Flushing, reasons flushTimeLimit={0} flushSizeLimit={1}.", flushTimeLimit, flushSizeLimit);

            return true;
        }

        /// <summary>
        /// Finds all changed records in the cache and persists them to the underlying coinview.
        /// </summary>
        /// <param name="force"><c>true</c> to enforce flush, <c>false</c> to flush only if <see cref="lastCacheFlushTime"/> is older than <see cref="CacheFlushTimeIntervalSeconds"/>.</param>
        public void Flush(bool force = true)
        {
            if (!force)
            {
                if (!this.ShouldFlush())
                    return;
            }

            // Before flushing the coinview persist the stake store
            // the stake store depends on the last block hash
            // to be stored after the stake store is persisted.
            if (this.stakeChainStore != null)
                this.stakeChainStore.Flush(true);

            // Before flushing the coinview persist the rewind data index store as well.
            if (this.rewindDataIndexCache != null)
                this.rewindDataIndexCache.SaveAndEvict(this.blockHash.Height, null);

            if (this.innerBlockHash == null)
                this.innerBlockHash = this.coindb.GetTipHash();

            lock (this.lockobj)
            {
                if (this.innerBlockHash == null)
                {
                    this.logger.LogTrace("(-)[NULL_INNER_TIP]");
                    return;
                }

                var modify = new List<UnspentOutput>();
                foreach (var cacheItem in this.cachedUtxoItems.Where(u => u.Value.IsDirty))
                {
                    cacheItem.Value.IsDirty = false;
                    cacheItem.Value.ExistInInner = true;

                    modify.Add(new UnspentOutput(cacheItem.Key, cacheItem.Value.Coins));
                }

                this.logger.LogDebug("Flushing {0} items.", modify.Count);

                this.coindb.SaveChanges(modify, this.innerBlockHash, this.blockHash, this.cachedRewindData.Select(c => c.Value).ToList());

                this.cachedRewindData.Clear();
                this.rewindDataSizeBytes = 0;
                this.dirtyCacheCount = 0;
                this.innerBlockHash = this.blockHash;
            }

            this.lastCacheFlushTime = this.dateTimeProvider.GetUtcNow();
        }

        /// <inheritdoc />
        public void SaveChanges(IList<UnspentOutput> outputs, HashHeightPair oldBlockHash, HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null)
        {
            Guard.NotNull(oldBlockHash, nameof(oldBlockHash));
            Guard.NotNull(nextBlockHash, nameof(nextBlockHash));
            Guard.NotNull(outputs, nameof(outputs));

            lock (this.lockobj)
            {
                if ((this.blockHash != null) && (oldBlockHash != this.blockHash))
                {
                    this.logger.LogDebug("{0}:'{1}'", nameof(this.blockHash), this.blockHash);
                    this.logger.LogTrace("(-)[BLOCKHASH_MISMATCH]");
                    throw new InvalidOperationException("Invalid oldBlockHash");
                }

                this.blockHash = nextBlockHash;
                long utxoSkipDisk = 0;

                var rewindData = new RewindData(oldBlockHash);
                Dictionary<OutPoint, int> indexItems = null;
                if (this.rewindDataIndexCache != null)
                    indexItems = new Dictionary<OutPoint, int>();

                foreach (UnspentOutput output in outputs)
                {
                    if (!this.cachedUtxoItems.TryGetValue(output.OutPoint, out CacheItem cacheItem))
                    {
                        // Add outputs to cache, this will happen for two cases
                        // 1. if a cached item was evicted
                        // 2. for new outputs that are added

                        if (output.CreatedFromBlock)
                        {
                            // if the output is indicate that it was added from a block
                            // There is no need to spend an extra call to disk.

                            this.logger.LogDebug("New Outpoint '{0}' created.", output.OutPoint);

                            cacheItem = new CacheItem()
                            {
                                ExistInInner = false,
                                IsDirty = false,
                                OutPoint = output.OutPoint,
                                Coins = null
                            };
                        }
                        else
                        {
                            // This can happen if the cached item was evicted while
                            // the block was being processed, fetch the output again from disk.

                            this.logger.LogDebug("Outpoint '{0}' is not found in cache, creating it.", output.OutPoint);

                            FetchCoinsResponse result = this.coindb.FetchCoins(new[] { output.OutPoint });
                            this.performanceCounter.AddMissCount(1);

                            UnspentOutput unspentOutput = result.UnspentOutputs.Single().Value;

                            cacheItem = new CacheItem()
                            {
                                ExistInInner = unspentOutput.Coins != null,
                                IsDirty = false,
                                OutPoint = unspentOutput.OutPoint,
                                Coins = unspentOutput.Coins
                            };
                        }

                        this.cachedUtxoItems.Add(cacheItem.OutPoint, cacheItem);
                        this.cacheSizeBytes += cacheItem.GetSize;
                        this.logger.LogDebug("CacheItem added to the cache during save '{0}'.", cacheItem.OutPoint);
                    }

                    // If output.Coins is null this means the utxo needs to be deleted
                    // otherwise this is a new utxo and we store it to cache.

                    if (output.Coins == null)
                    {
                        // DELETE COINS

                        // In cases of an output spent in the same block
                        // it wont exist in cash or in disk so its safe to remove it
                        if (cacheItem.Coins == null)
                        {
                            if (cacheItem.ExistInInner)
                                throw new InvalidOperationException(string.Format("Missmtch between coins in cache and in disk for output {0}", cacheItem.OutPoint));
                        }
                        else
                        {
                            // Handle rewind data
                            this.logger.LogDebug("Create restore outpoint '{0}' in OutputsToRestore rewind data.", cacheItem.OutPoint);
                            rewindData.OutputsToRestore.Add(new RewindDataOutput(cacheItem.OutPoint, cacheItem.Coins));
                            rewindData.TotalSize += cacheItem.GetSize;

                            if (this.rewindDataIndexCache != null && indexItems != null)
                            {
                                indexItems[cacheItem.OutPoint] = this.blockHash.Height;
                            }
                        }

                        // If a spent utxo never made it to disk then no need to keep it in memory.
                        if (!cacheItem.ExistInInner)
                        {
                            this.logger.LogDebug("Utxo '{0}' is not in disk, removing from cache.", cacheItem.OutPoint);
                            this.cachedUtxoItems.Remove(cacheItem.OutPoint);
                            this.cacheSizeBytes -= cacheItem.GetSize;
                            utxoSkipDisk++;
                            if (cacheItem.IsDirty) this.dirtyCacheCount--;
                        }
                        else
                        {
                            // Now modify the cached items with the mutated data.
                            this.logger.LogDebug("Mark cache item '{0}' as spent .", cacheItem.OutPoint);

                            this.cacheSizeBytes -= cacheItem.GetScriptSize;
                            cacheItem.Coins = null;

                            // Delete output from cache but keep a the cache
                            // item reference so it will get deleted form disk

                            cacheItem.IsDirty = true;
                            this.dirtyCacheCount++;
                        }
                    }
                    else
                    {
                        // ADD COINS

                        if (cacheItem.Coins != null)
                        {
                            // Allow overrides.
                            // See https://github.com/bitcoin/bitcoin/blob/master/src/coins.cpp#L94

                            bool allowOverride = cacheItem.Coins.IsCoinbase && output.Coins != null;

                            if (!allowOverride)
                            {
                                throw new InvalidOperationException(string.Format("New coins override coins in cache or store, for output '{0}'", cacheItem.OutPoint));
                            }

                            this.logger.LogDebug("Coin override alllowed for utxo '{0}'.", cacheItem.OutPoint);

                            // Deduct the current script size form the
                            // total cache size, it will be added again later.
                            this.cacheSizeBytes -= cacheItem.GetScriptSize;

                            // Clear this in order to calculate the cache size
                            // this will get set later when overridden
                            cacheItem.Coins = null;
                        }

                        // Handle rewind data
                        // New trx so it needs to be deleted if a rewind happens.
                        this.logger.LogDebug("Adding output '{0}' to TransactionsToRemove rewind data.", cacheItem.OutPoint);
                        rewindData.OutputsToRemove.Add(cacheItem.OutPoint);
                        rewindData.TotalSize += cacheItem.GetSize;

                        // Put in the cache the new UTXOs.
                        this.logger.LogDebug("Mark cache item '{0}' as new .", cacheItem.OutPoint);

                        cacheItem.Coins = output.Coins;
                        this.cacheSizeBytes += cacheItem.GetScriptSize;

                        // Mark the cache item as dirty so it get persisted
                        // to disk and not evicted form cache

                        cacheItem.IsDirty = true;
                        this.dirtyCacheCount++;
                    }
                }

                this.performanceCounter.AddUtxoSkipDiskCount(utxoSkipDisk);

                if (this.rewindDataIndexCache != null && indexItems.Any())
                {
                    this.rewindDataIndexCache.SaveAndEvict(this.blockHash.Height, indexItems);
                }

                // Add the most recent rewind data to the cache.
                this.cachedRewindData.Add(this.blockHash.Height, rewindData);
                this.rewindDataSizeBytes += rewindData.TotalSize;

                // Remove rewind data form the back of a moving window.
                // The closer we get to the tip we keep a longer rewind data window.
                // Anything bellow last checkpoint we keep the minimal of 10
                // (random low number) rewind data items.
                // Beyond last checkpoint:
                // - For POS we keep a window of MaxReorg.
                // - For POW we keep 100 items (possibly better is an algo that grows closer to tip)

                // A moving window of information needed to rewind the node to a previous block.
                // When cache is flushed the rewind data will allow to rewind the node up to the
                // number of rewind blocks.
                // TODO: move rewind data to use block store.
                // Rewind data can go away all together if the node uses the blocks in block store
                // to get the rewind information, blockstore persists much more frequent then coin cache
                // So using block store for rewinds is not entirely impossible.

                int rewindDataWindow = this.CalculateRewindWindow();

                int rewindToRemove = this.blockHash.Height - (int)rewindDataWindow;

                if (this.cachedRewindData.TryGetValue(rewindToRemove, out RewindData delete))
                {
                    this.logger.LogDebug("Remove rewind data height '{0}' from cache.", rewindToRemove);
                    this.cachedRewindData.Remove(rewindToRemove);
                    this.rewindDataSizeBytes -= delete.TotalSize;
                }
            }
        }

        /// <summary>
        /// Calculate the window of how many rewind items to keep in memory.
        /// </summary>
        /// <returns></returns>
        public int CalculateRewindWindow()
        {
            uint rewindDataWindow = 10;

            if (this.blockHash.Height >= this.lastCheckpointHeight)
            {
                if (this.network.Consensus.MaxReorgLength != 0)
                {
                    rewindDataWindow = this.network.Consensus.MaxReorgLength + 1;
                }
                else
                {
                    // TODO: make the rewind data window a configuration
                    // parameter of every a network parameter.

                    // For POW assume BTC where a rewind data of 100 is more then enough.
                    rewindDataWindow = 100;
                }
            }

            return (int)rewindDataWindow;
        }

        public HashHeightPair Rewind()
        {
            if (this.innerBlockHash == null)
            {
                this.innerBlockHash = this.coindb.GetTipHash();
            }

            // Flush the entire cache before rewinding
            this.Flush(true);

            lock (this.lockobj)
            {
                HashHeightPair hash = this.coindb.Rewind();

                foreach (KeyValuePair<OutPoint, CacheItem> cachedUtxoItem in this.cachedUtxoItems)
                {
                    // This is a protection check to ensure we are
                    // not deleting dirty items form the cache.

                    if (cachedUtxoItem.Value.IsDirty)
                        throw new InvalidOperationException("Items in cache are modified");
                }

                // All the cached utxos are now on disk so we can clear the cached entry list.
                this.cachedUtxoItems.Clear();
                this.cacheSizeBytes = 0;
                this.dirtyCacheCount = 0;

                this.innerBlockHash = hash;
                this.blockHash = hash;

                if (this.rewindDataIndexCache != null)
                    this.rewindDataIndexCache.Initialize(this.blockHash.Height, this);

                return hash;
            }
        }

        /// <inheritdoc />
        public RewindData GetRewindData(int height)
        {
            if (this.cachedRewindData.TryGetValue(height, out RewindData existingRewindData))
                return existingRewindData;

            return this.coindb.GetRewindData(height);
        }

        private void AddBenchStats(StringBuilder log)
        {
            log.AppendLine("======CachedCoinView Bench======");
            DateTime now = this.dateTimeProvider.GetUtcNow();
            var lastFlush = (now - this.lastCacheFlushTime).TotalMinutes;
            log.AppendLine("Last flush ".PadRight(20) + Math.Round(lastFlush, 2) + " min ago (flush every " + TimeSpan.FromSeconds(this.CacheFlushTimeIntervalSeconds).TotalMinutes + " min)");

            log.AppendLine("Coin cache tip ".PadRight(20) + this.blockHash.Height);
            log.AppendLine("Coin store tip ".PadRight(20) + this.innerBlockHash.Height);
            log.AppendLine("block store tip ".PadRight(20) + "tbd");
            log.AppendLine();

            log.AppendLine("Cache entries ".PadRight(20) + this.cacheCount + " items");
            log.AppendLine("Dirty cache entries ".PadRight(20) + this.dirtyCacheCount + " items");

            log.AppendLine("Rewind data entries ".PadRight(20) + this.rewindDataCount + " items");
            var cache = this.cacheSizeBytes;
            var rewind = this.rewindDataSizeBytes;
            double filledPercentage = Math.Round(((cache + rewind) / (double)this.MaxCacheSizeBytes) * 100, 2);
            log.AppendLine("Cache size".PadRight(20) + cache.BytesToMegaBytes() + " MB");
            log.AppendLine("Rewind data size".PadRight(20) + rewind.BytesToMegaBytes() + " MB");
            log.AppendLine("Total cache size".PadRight(20) + (cache + rewind).BytesToMegaBytes() + " MB / " + this.consensusSettings.MaxCoindbCacheInMB + " MB (" + filledPercentage + "%)");

            CachePerformanceSnapshot snapShot = this.performanceCounter.Snapshot();

            if (this.latestPerformanceSnapShot == null)
                log.AppendLine(snapShot.ToString());
            else
                log.AppendLine((snapShot - this.latestPerformanceSnapShot).ToString());

            this.latestPerformanceSnapShot = snapShot;
        }
    }
}