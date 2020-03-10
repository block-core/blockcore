using System;
using System.Text;
using System.Threading;
using Blockcore.Configuration.Logging;
using Blockcore.Utilities;

namespace Blockcore.Features.Consensus.CoinViews
{
    /// <summary>
    /// Statistics to measure the hit rate of the cache.
    /// </summary>
    public class CachePerformanceCounter
    {
        /// <summary>UTC timestamp when the performance counter was created.</summary>
        private DateTime start;

        /// <summary>UTC timestamp when the performance counter was created.</summary>
        public DateTime Start
        {
            get { return this.start; }
        }

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        private long missCount;

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        public long MissCount
        {
            get { return this.missCount; }
        }

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        private long hitCount;

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        public long HitCount
        {
            get { return this.hitCount; }
        }

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        private long missCountCache;

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        public long MissCountCache
        {
            get { return this.missCountCache; }
        }

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        private long hitCountCache;

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        public long HitCountCache
        {
            get { return this.hitCountCache; }
        }

        /// <summary>Time span since the performance counter was created.</summary>
        public TimeSpan Elapsed
        {
            get
            {
                return this.dateTimeProvider.GetUtcNow() - this.Start;
            }
        }

        /// <summary>Number of utxos that never got flushed to disk.</summary>
        private long utxoSkipDisk;

        /// <summary>Number of utxos that never got flushed to disk.</summary>
        public long UtxoSkipDisk
        {
            get { return this.utxoSkipDisk; }
        }

        /// <summary>Provider of date time functionality.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>
        /// Initializes an instance of the object.
        /// </summary>
        /// <param name="dateTimeProvider">Provider of date time functionality.</param>
        public CachePerformanceCounter(IDateTimeProvider dateTimeProvider)
        {
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));

            this.dateTimeProvider = dateTimeProvider;
            this.start = this.dateTimeProvider.GetUtcNow();
        }

        /// <summary>
        /// Adds new sample to the number of missed cache queries.
        /// </summary>
        /// <param name="count">Number of missed queries to add.</param>
        public void AddMissCount(long count)
        {
            Interlocked.Add(ref this.missCount, count);
        }

        /// <summary>
        /// Adds new sample to the number of hit cache queries.
        /// </summary>
        /// <param name="count">Number of hit queries to add.</param>
        public void AddHitCount(long count)
        {
            Interlocked.Add(ref this.hitCount, count);
        }

        /// <summary>
        /// Adds new sample to the number of missed cache queries.
        /// </summary>
        /// <param name="count">Number of missed queries to add.</param>
        public void AddCacheMissCount(long count)
        {
            Interlocked.Add(ref this.missCountCache, count);
        }

        /// <summary>
        /// Adds new sample to the number of hit cache queries.
        /// </summary>
        /// <param name="count">Number of hit queries to add.</param>
        public void AddCacheHitCount(long count)
        {
            Interlocked.Add(ref this.hitCountCache, count);
        }

        /// <summary>
        /// Adds new sample to the number of utxo that never got flushed.
        /// </summary>
        /// <param name="count">Number of hit queries to add.</param>
        public void AddUtxoSkipDiskCount(long count)
        {
            Interlocked.Add(ref this.utxoSkipDisk, count);
        }

        /// <summary>
        /// Creates a snapshot of the current state of the performance counter.
        /// </summary>
        /// <returns>Newly created snapshot.</returns>
        public CachePerformanceSnapshot Snapshot()
        {
            var snap = new CachePerformanceSnapshot(this.missCount, this.hitCount, this.missCountCache, this.hitCountCache, this.utxoSkipDisk)
            {
                Start = this.Start,
                // TODO: Would it not be better for these two guys to be part of the constructor? Either implicitly or explicitly.
                Taken = this.dateTimeProvider.GetUtcNow()
            };
            return snap;
        }
    }

    /// <summary>
    /// Snapshot of a state of a performance counter taken at a certain time.
    /// </summary>
    public class CachePerformanceSnapshot
    {
        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        private readonly long hitCount;

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        public long TotalHitCount
        {
            get { return this.hitCount; }
        }

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        private readonly long missCount;

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        public long TotalMissCount
        {
            get { return this.missCount; }
        }

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        private readonly long hitCountCache;

        /// <summary>Number of cache queries for which the result was found in the cache.</summary>
        public long TotalHitCountCache
        {
            get { return this.hitCountCache; }
        }

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        private readonly long missCountCache;

        /// <summary>Number of cache queries for which the result was not found in the cache.</summary>
        public long TotalMissCountCache
        {
            get { return this.missCountCache; }
        }

        private readonly long utxoSkipDisk;

        public long TotalUtxoSkipDisk
        {
            get { return this.utxoSkipDisk; }
        }

        /// <summary>UTC timestamp when the snapshotted performance counter was created.</summary>
        public DateTime Start { get; internal set; }

        /// <summary>UTC timestamp when the snapshot was taken.</summary>
        public DateTime Taken { get; internal set; }

        /// <summary>Time span between the creation of the performance counter and the creation of its snapshot.</summary>
        public TimeSpan Elapsed
        {
            get
            {
                return this.Taken - this.Start;
            }
        }

        public CachePerformanceSnapshot(long missCount, long hitCount, long missCountCache, long hitCountCache, long utxoSkipDisk)
        {
            this.missCount = missCount;
            this.hitCount = hitCount;
            this.hitCountCache = hitCountCache;
            this.missCountCache = missCountCache;
            this.utxoSkipDisk = utxoSkipDisk;
        }

        /// <summary>
        /// Creates a snapshot based on difference of two performance counter snapshots.
        /// <para>
        /// This is used to obtain statistic information about performance of the cache
        /// during certain period.</para>
        /// </summary>
        /// <param name="end">Newer performance counter snapshot.</param>
        /// <param name="start">Older performance counter snapshot.</param>
        /// <returns>Snapshot of the difference between the two performance counter snapshots.</returns>
        /// <remarks>The two snapshots should be taken from a single performance counter.
        /// Otherwise the start times of the snapshots will be different, which is not allowed.</remarks>
        public static CachePerformanceSnapshot operator -(CachePerformanceSnapshot end, CachePerformanceSnapshot start)
        {
            if (end.Start != start.Start)
                throw new InvalidOperationException("Performance snapshot should be taken from the same point of time");

            if (end.Taken < start.Taken)
                throw new InvalidOperationException("The difference of snapshot can't be negative");

            long missCount = end.missCount - start.missCount;
            long hitCount = end.hitCount - start.hitCount;
            long missCountCache = end.missCountCache - start.missCountCache;
            long hitCountCache = end.hitCountCache - start.hitCountCache;

            long utxoNotFlushed = end.utxoSkipDisk - start.utxoSkipDisk;

            return new CachePerformanceSnapshot(missCount, hitCount, missCountCache, hitCountCache, utxoNotFlushed)
            {
                Start = start.Taken,
                Taken = end.Taken
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            long total = this.TotalMissCount + this.TotalHitCount;
            long totalCache = this.TotalMissCountCache + this.TotalHitCountCache;
            long totalInsert = this.TotalMissCount + this.TotalMissCountCache;

            var builder = new StringBuilder();
            builder.AppendLine("====Cache Stats(%)====");
            if (total != 0)
            {
                if (totalCache > 0) builder.AppendLine("Prefetch cache:".PadRight(LoggingConfiguration.ColumnLength) + "hit: " + ((decimal)this.TotalHitCountCache * 100m / totalCache).ToString("0.00") + "% miss:" + ((decimal)this.TotalMissCountCache * 100m / totalCache).ToString("0.00") + "%");
                if (total > 0) builder.AppendLine("Fetch cache:".PadRight(LoggingConfiguration.ColumnLength) + "hit: " + ((decimal)this.TotalHitCount * 100m / total).ToString("0.00") + "% miss:" + ((decimal)this.TotalMissCount * 100m / total).ToString("0.00") + "%");
                if (totalInsert > 0) builder.AppendLine("Utxo skip disk:".PadRight(LoggingConfiguration.ColumnLength) + ((decimal)this.TotalUtxoSkipDisk).ToString() + "(" + ((decimal)this.TotalUtxoSkipDisk * 100m / totalInsert).ToString("0.00") + "%)");
            }

            builder.AppendLine("========================");
            return builder.ToString();
        }
    }
}