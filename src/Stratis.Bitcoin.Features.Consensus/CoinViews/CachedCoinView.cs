using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus.ProvenBlockHeaders;
using Stratis.Bitcoin.Utilities;
using TracerAttributes;

namespace Stratis.Bitcoin.Features.Consensus.CoinViews
{
    /// <summary>
    /// Cache layer for coinview prevents too frequent updates of the data in the underlying storage.
    /// </summary>
    public class CachedCoinView : ICoinView, IBackedCoinView
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
        }

        /// <summary>Default maximum number of transactions in the cache.</summary>
        public const int CacheMaxItemsDefault = 100000;

        /// <summary>Length of the coinview cache flushing interval in seconds.</summary>
        /// <seealso cref="lastCacheFlushTime"/>
        public const int CacheFlushTimeIntervalSeconds = 60;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Maximum number of transactions in the cache.</summary>
        public int MaxItems { get; set; }

        /// <summary>Statistics of hits and misses in the cache.</summary>
        private CachePerformanceCounter performanceCounter { get; set; }

        /// <summary>Lock object to protect access to <see cref="cachedUtxoItems"/>, <see cref="blockHash"/>, <see cref="cachedRewindDataIndex"/>, and <see cref="innerBlockHash"/>.</summary>
        private readonly object lockobj;

        /// <summary>Hash of the block headers of the tip of the coinview.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private HashHeightPair blockHash;

        /// <summary>Hash of the block headers of the tip of the underlaying coinview.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private HashHeightPair innerBlockHash;

        /// <summary>Coin view at one layer below this implementaiton.</summary>
        private readonly ICoinView inner;

        /// <summary>Pending list of rewind data to be persisted to a persistent storage.</summary>
        /// <remarks>All access to this list has to be protected by <see cref="lockobj"/>.</remarks>
        private readonly SortedDictionary<int, RewindData> cachedRewindDataIndex;

        /// <inheritdoc />
        public ICoinView Inner => this.inner;

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
        private int cacheEntryCount => this.cachedUtxoItems.Count;

        /// <summary>Provider of time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Time of the last cache flush.</summary>
        private DateTime lastCacheFlushTime;

        private CachePerformanceSnapshot latestPerformanceSnapShot;

        private readonly Random random;

        /// <summary>
        /// Initializes instance of the object based on DBreeze based coinview.
        /// </summary>
        /// <param name="inner">Underlying coinview with database storage.</param>
        /// <param name="dateTimeProvider">Provider of time functions.</param>
        /// <param name="loggerFactory">Factory to be used to create logger for the puller.</param>
        /// <param name="nodeStats">The node stats.</param>
        /// <param name="stakeChainStore">Storage of POS block information.</param>
        /// <param name="rewindDataIndexCache">Rewind data index store.</param>
        public CachedCoinView(DBreezeCoinView inner, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory, INodeStats nodeStats, StakeChainStore stakeChainStore = null, IRewindDataIndexCache rewindDataIndexCache = null) :
            this(dateTimeProvider, loggerFactory, nodeStats, stakeChainStore, rewindDataIndexCache)
        {
            Guard.NotNull(inner, nameof(inner));
            this.inner = inner;
        }

        /// <summary>
        /// Initializes instance of the object based on memory based coinview.
        /// </summary>
        /// <param name="inner">Underlying coinview with memory based storage.</param>
        /// <param name="dateTimeProvider">Provider of time functions.</param>
        /// <param name="loggerFactory">Factory to be used to create logger for the puller.</param>
        /// <param name="nodeStats">The node stats.</param>
        /// <param name="stakeChainStore">Storage of POS block information.</param>
        /// <param name="rewindDataIndexCache">Rewind data index store.</param>
        /// <remarks>
        /// This is used for testing the coinview.
        /// It allows a coin view that only has in-memory entries.
        /// </remarks>
        public CachedCoinView(InMemoryCoinView inner, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory, INodeStats nodeStats, StakeChainStore stakeChainStore = null, IRewindDataIndexCache rewindDataIndexCache = null) :
            this(dateTimeProvider, loggerFactory, nodeStats, stakeChainStore, rewindDataIndexCache)
        {
            Guard.NotNull(inner, nameof(inner));
            this.inner = inner;
        }

        /// <summary>
        /// Initializes instance of the object based.
        /// </summary>
        /// <param name="dateTimeProvider">Provider of time functions.</param>
        /// <param name="loggerFactory">Factory to be used to create logger for the puller.</param>
        /// <param name="nodeStats">The node stats.</param>
        /// <param name="stakeChainStore">Storage of POS block information.</param>
        /// <param name="rewindDataIndexCache">Rewind data index store.</param>
        private CachedCoinView(IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory, INodeStats nodeStats, StakeChainStore stakeChainStore = null, IRewindDataIndexCache rewindDataIndexCache = null)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.dateTimeProvider = dateTimeProvider;
            this.stakeChainStore = stakeChainStore;
            this.rewindDataIndexCache = rewindDataIndexCache;
            this.MaxItems = CacheMaxItemsDefault;
            this.lockobj = new object();
            this.cachedUtxoItems = new Dictionary<OutPoint, CacheItem>();
            this.performanceCounter = new CachePerformanceCounter(this.dateTimeProvider);
            this.lastCacheFlushTime = this.dateTimeProvider.GetUtcNow();
            this.cachedRewindDataIndex = new SortedDictionary<int, RewindData>();
            this.random = new Random();

            nodeStats.RegisterStats(this.AddBenchStats, StatsType.Benchmark, this.GetType().Name, 300);
        }

        public HashHeightPair GetTipHash()
        {
            if (this.blockHash == null)
            {
                HashHeightPair response = this.inner.GetTipHash();

                this.innerBlockHash = response;
                this.blockHash = this.innerBlockHash;
            }

            return this.blockHash;
        }

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
                    FetchCoinsResponse fetchedCoins = this.Inner.FetchCoins(missedOutpoint.ToArray());

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

                        this.logger.LogDebug("CacheItem added to the cache, Transaction Id '{0}', UTXO:'{1}'.", cache.OutPoint, cache.Coins);
                        this.cachedUtxoItems.Add(cache.OutPoint, cache);
                    }
                }

                int cacheEntryCount = this.cacheEntryCount;
                if (cacheEntryCount > this.MaxItems)
                {
                    this.logger.LogDebug("Cache is full now with {0} entries, evicting.", cacheEntryCount);
                    this.EvictLocked();
                }
            }

            return result;
        }

        /// <summary>
        /// Finds all changed records in the cache and persists them to the underlying coinview.
        /// </summary>
        /// <param name="force"><c>true</c> to enforce flush, <c>false</c> to flush only if <see cref="lastCacheFlushTime"/> is older than <see cref="CacheFlushTimeIntervalSeconds"/>.</param>
        /// <remarks>
        /// WARNING: This method can only be run from <see cref="ConsensusLoop.Execute(System.Threading.CancellationToken)"/> thread context
        /// or when consensus loop is stopped. Otherwise, there is a risk of race condition when the consensus loop accepts new block.
        /// </remarks>
        public void Flush(bool force = true)
        {
            DateTime now = this.dateTimeProvider.GetUtcNow();
            if (!force && ((now - this.lastCacheFlushTime).TotalSeconds < CacheFlushTimeIntervalSeconds))
            {
                this.logger.LogTrace("(-)[NOT_NOW]");
                return;
            }

            // Before flushing the coinview persist the stake store
            // the stake store depends on the last block hash
            // to be stored after the stake store is persisted.
            if (this.stakeChainStore != null)
                this.stakeChainStore.Flush(true);

            // Before flushing the coinview persist the rewind data index store as well.
            if (this.rewindDataIndexCache != null)
                this.rewindDataIndexCache.Flush(this.blockHash.Height);

            if (this.innerBlockHash == null)
                this.innerBlockHash = this.inner.GetTipHash();

            lock (this.lockobj)
            {
                if (this.innerBlockHash == null)
                {
                    this.logger.LogTrace("(-)[NULL_INNER_TIP]");
                    return;
                }

                var modify = new List<UnspentOutput>();
                foreach(var cacheItem in this.cachedUtxoItems.Where(u => u.Value.IsDirty))
                {
                    cacheItem.Value.IsDirty = false;
                    cacheItem.Value.ExistInInner = true;

                    modify.Add(new UnspentOutput(cacheItem.Key, cacheItem.Value.Coins));
                }

                this.Inner.SaveChanges(modify, this.innerBlockHash, this.blockHash, this.cachedRewindDataIndex.Select(c => c.Value).ToList());

                this.cachedRewindDataIndex.Clear();
                this.innerBlockHash = this.blockHash;
            }

            this.lastCacheFlushTime = this.dateTimeProvider.GetUtcNow();
        }

        /// <summary>
        /// Deletes some items from the cache to free space for new items.
        /// Only items that are persisted in the underlaying storage can be deleted from the cache.
        /// </summary>
        /// <remarks>Should be protected by <see cref="lockobj"/>.</remarks>
        private void EvictLocked()
        {
            foreach (KeyValuePair<OutPoint, CacheItem> entry in this.cachedUtxoItems.ToList())
            {
                if (!entry.Value.IsDirty && entry.Value.ExistInInner)
                {
                    if ((this.random.Next() % 3) == 0)
                    {
                        this.logger.LogDebug("Transaction Id '{0}' selected to be removed from the cache, CacheItem:'{1}'.", entry.Key, entry.Value.Coins);
                        this.cachedUtxoItems.Remove(entry.Key);
                    }
                }
            }
        }

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
                var rewindData = new RewindData(oldBlockHash);
                var indexItems = new Dictionary<OutPoint, int>();

                foreach (UnspentOutput output in outputs)
                {
                    if (!this.cachedUtxoItems.TryGetValue(output.OutPoint, out CacheItem cacheItem))
                    {
                       // Add outputs to cache, this will happen for two cases
                       // 1. if a chaced item was evicted
                       // 2. for new outputs that are added

                        this.logger.LogDebug("Outpoint '{0}' is not found in cache, creating it.", output.OutPoint);

                        FetchCoinsResponse result = this.inner.FetchCoins(new[] { output.OutPoint });

                        UnspentOutput unspentOutput = result.UnspentOutputs.Single().Value;

                        cacheItem = new CacheItem()
                        {
                            ExistInInner = unspentOutput.Coins != null,
                            IsDirty = false,
                            OutPoint = unspentOutput.OutPoint,
                            Coins = unspentOutput.Coins
                        };

                        this.cachedUtxoItems.Add(cacheItem.OutPoint, cacheItem);
                        this.logger.LogDebug("CacheItem added to the cache during save '{0}'.", cacheItem.OutPoint);
                    }

                    // An output must always have coins even if they are spent, 
                    // so the coins can be added to the rewind data list

                    if (output.Coins == null)
                        throw new InvalidOperationException(string.Format("Missing coins for output {0}", output.OutPoint));

                    // If output.Spent is true this means the utxo deleted and it's removed from cache
                    // otherwise this is a new utxo and we store it to cache.

                    if (output.Spent)
                    {
                        // Now modify the cached items with the mutated data.
                        this.logger.LogDebug("Mark cache item '{0}' as spent .", cacheItem.OutPoint);

                        cacheItem.Coins = null;

                        // If a spent utxo never made it to disk then no need to keep it in memory.
                        if (!cacheItem.ExistInInner)
                        {
                            this.logger.LogDebug("Utxo '{0}' is not in disk, removing from cache.", cacheItem.OutPoint);
                            this.cachedUtxoItems.Remove(cacheItem.OutPoint);
                        }
                    }
                    else
                    {
                        // Put in the cache the new UTXOs.
                        this.logger.LogDebug("Mark cache item '{0}' as new .", cacheItem.OutPoint);
                        cacheItem.Coins = output.Coins;
                    }

                    cacheItem.IsDirty = true;

                    // If tip is bellow last checkpoint there is no need to keep rewind data
                    // However the way the fullnode is built, occasionaly on shutdown if block store is behind
                    // consensus will reorg to match block store.
                    // We can prevent that by using the same underline storage for utxo and blocks
                    // Or only persist to disk the last x blocks of rewind data.
                    if (true) 
                    {
                        if (output.Spent)
                        {
                            this.logger.LogDebug("Create restore outpoint '{0}' in OutputsToRestore rewind data.", cacheItem.OutPoint);
                            rewindData.OutputsToRestore.Add(new RewindDataOutput(output.OutPoint, output.Coins));

                            if (this.rewindDataIndexCache != null)
                            {
                                indexItems[output.OutPoint] = this.blockHash.Height;
                            }
                        }
                        else
                        {
                            // New trx so it needs to be deleted if a rewind happens.
                            this.logger.LogDebug("Adding output '{0}' to TransactionsToRemove rewind data.", output.OutPoint);
                            rewindData.OutputsToRemove.Add(output.OutPoint);
                        }
                    }
                }

                if (this.rewindDataIndexCache != null && indexItems.Any())
                {
                    this.rewindDataIndexCache.Save(indexItems);
                    this.rewindDataIndexCache.Flush(this.blockHash.Height);
                }

                this.cachedRewindDataIndex.Add(this.blockHash.Height, rewindData);
            }
        }

        public HashHeightPair Rewind()
        {
            if (this.innerBlockHash == null)
            {
                this.innerBlockHash = this.inner.GetTipHash();
            }

            // Flush the entire cache before rewinding
            this.Flush(true);

            lock (this.lockobj)
            {
                HashHeightPair hash = this.inner.Rewind();

                foreach (KeyValuePair<OutPoint, CacheItem> cachedUtxoItem in this.cachedUtxoItems)
                {
                    // This is a protection check to ensure we are
                    // not deleting dirty items form the cache.

                    if (cachedUtxoItem.Value.IsDirty)
                        throw new InvalidOperationException("Items in cache are modified");
                }

                // All the cached utxos are now on disk so we can clear the cached entry list.
                this.cachedUtxoItems.Clear();

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
            if (this.cachedRewindDataIndex.TryGetValue(height, out RewindData existingRewindData))
                return existingRewindData;

            return this.Inner.GetRewindData(height);
        }

        [NoTrace]
        private void AddBenchStats(StringBuilder log)
        {
            log.AppendLine("======CachedCoinView Bench======");

            log.AppendLine("Cache entries".PadRight(20) + this.cacheEntryCount);

            CachePerformanceSnapshot snapShot = this.performanceCounter.Snapshot();

            if (this.latestPerformanceSnapShot == null)
                log.AppendLine(snapShot.ToString());
            else
                log.AppendLine((snapShot - this.latestPerformanceSnapShot).ToString());

            this.latestPerformanceSnapShot = snapShot;
        }
    }
}
