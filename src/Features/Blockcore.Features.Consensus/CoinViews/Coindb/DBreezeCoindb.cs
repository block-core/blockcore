using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Blockcore.Configuration;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks;
using Blockcore.Utilities;
using DBreeze;
using DBreeze.DataTypes;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.CoinViews.Coindb
{
    /// <summary>
    /// Persistent implementation of coinview using dBreeze database.
    /// </summary>
    public class DBreezeCoindb : ICoindb, IStakdb, IDisposable
    {
        /// <summary>Database key under which the block hash of the coin view's current tip is stored.</summary>
        private static readonly byte[] blockHashKey = new byte[0];

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>Hash of the block which is currently the tip of the coinview.</summary>
        private HashHeightPair blockHash;

        /// <summary>Performance counter to measure performance of the database insert and query operations.</summary>
        private readonly BackendPerformanceCounter performanceCounter;

        private BackendPerformanceSnapshot latestPerformanceSnapShot;

        /// <summary>Access to dBreeze database.</summary>
        private readonly DBreezeEngine dBreeze;

        private DataStoreSerializer dataStoreSerializer;

        public DBreezeCoindb(Network network, DataFolder dataFolder, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory, INodeStats nodeStats, DataStoreSerializer dataStoreSerializer)
            : this(network, dataFolder.CoindbPath, dateTimeProvider, loggerFactory, nodeStats, dataStoreSerializer)
        {
        }

        public DBreezeCoindb(Network network, string folder, IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory, INodeStats nodeStats, DataStoreSerializer dataStoreSerializer)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotEmpty(folder, nameof(folder));

            this.dataStoreSerializer = dataStoreSerializer;

            // Create the coinview folder if it does not exist.
            Directory.CreateDirectory(folder);

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.dBreeze = new DBreezeEngine(folder);
            this.network = network;
            this.performanceCounter = new BackendPerformanceCounter(dateTimeProvider);

            nodeStats.RegisterStats(this.AddBenchStats, StatsType.Benchmark, this.GetType().Name, 400);
        }

        public void Initialize()
        {
            Block genesis = this.network.GetGenesis();

            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.ValuesLazyLoadingIsOn = false;
                transaction.SynchronizeTables("BlockHash");

                if (this.GetTipHash(transaction) == null)
                {
                    this.SetBlockHash(transaction, new HashHeightPair(genesis.GetHash(), 0));

                    // Genesis coin is unspendable so do not add the coins.
                    transaction.Commit();
                }
            }
        }

        public HashHeightPair GetTipHash()
        {
            HashHeightPair tipHash;

            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.ValuesLazyLoadingIsOn = false;
                tipHash = this.GetTipHash(transaction);
            }

            return tipHash;
        }

        public FetchCoinsResponse FetchCoins(OutPoint[] utxos)
        {
            FetchCoinsResponse res = new FetchCoinsResponse();
            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.SynchronizeTables("BlockHash", "Coins");
                transaction.ValuesLazyLoadingIsOn = false;

                using (new StopwatchDisposable(o => this.performanceCounter.AddQueryTime(o)))
                {
                    this.performanceCounter.AddQueriedEntities(utxos.Length);

                    foreach (OutPoint outPoint in utxos)
                    {
                        Row<byte[], byte[]> row = transaction.Select<byte[], byte[]>("Coins", outPoint.ToBytes());
                        Coins outputs = row.Exists ? this.dataStoreSerializer.Deserialize<Coins>(row.Value) : null;

                        this.logger.LogDebug("Outputs for '{0}' were {1}.", outPoint, outputs == null ? "NOT loaded" : "loaded");

                        res.UnspentOutputs.Add(outPoint, new UnspentOutput(outPoint, outputs));
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Obtains a block header hash of the coinview's current tip.
        /// </summary>
        /// <param name="transaction">Open dBreeze transaction.</param>
        /// <returns>Block header hash of the coinview's current tip.</returns>
        private HashHeightPair GetTipHash(DBreeze.Transactions.Transaction transaction)
        {
            if (this.blockHash == null)
            {
                Row<byte[], byte[]> row = transaction.Select<byte[], byte[]>("BlockHash", blockHashKey);
                if (row.Exists)
                {
                    this.blockHash = new HashHeightPair();
                    this.blockHash.FromBytes(row.Value);
                }
            }

            return this.blockHash;
        }

        private void SetBlockHash(DBreeze.Transactions.Transaction transaction, HashHeightPair nextBlockHash)
        {
            this.blockHash = nextBlockHash;
            transaction.Insert<byte[], byte[]>("BlockHash", blockHashKey, nextBlockHash.ToBytes());
        }

        public void SaveChanges(IList<UnspentOutput> unspentOutputs, HashHeightPair oldBlockHash, HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null)
        {
            int insertedEntities = 0;

            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.ValuesLazyLoadingIsOn = false;
                transaction.SynchronizeTables("BlockHash", "Coins", "Rewind");

                // Speed can degrade when keys are in random order and, especially, if these keys have high entropy.
                // This settings helps with speed, see dBreeze documentations about details.
                // We should double check if this settings help in our scenario, or sorting keys and operations is enough.
                // Refers to issue #2483. https://github.com/stratisproject/StratisBitcoinFullNode/issues/2483
                transaction.Technical_SetTable_OverwriteIsNotAllowed("Coins");

                using (new StopwatchDisposable(o => this.performanceCounter.AddInsertTime(o)))
                {
                    HashHeightPair current = this.GetTipHash(transaction);
                    if (current != oldBlockHash)
                    {
                        this.logger.LogTrace("(-)[BLOCKHASH_MISMATCH]");
                        throw new InvalidOperationException("Invalid oldBlockHash");
                    }

                    this.SetBlockHash(transaction, nextBlockHash);

                    // Here we'll add items to be inserted in a second pass.
                    List<UnspentOutput> toInsert = new List<UnspentOutput>();

                    foreach (var coin in unspentOutputs.OrderBy(utxo => utxo.OutPoint, new OutPointComparer()))
                    {
                        if (coin.Coins == null)
                        {
                            this.logger.LogDebug("Outputs of transaction ID '{0}' are prunable and will be removed from the database.", coin.OutPoint);
                            transaction.RemoveKey("Coins", coin.OutPoint.ToBytes());
                        }
                        else
                        {
                            // Add the item to another list that will be used in the second pass.
                            // This is for performance reasons: dBreeze is optimized to run the same kind of operations, sorted.
                            toInsert.Add(coin);
                        }
                    }

                    for (int i = 0; i < toInsert.Count; i++)
                    {
                        var coin = toInsert[i];
                        this.logger.LogDebug("Outputs of transaction ID '{0}' are NOT PRUNABLE and will be inserted into the database. {1}/{2}.", coin.OutPoint, i, toInsert.Count);

                        transaction.Insert("Coins", coin.OutPoint.ToBytes(), this.dataStoreSerializer.Serialize(coin.Coins));
                    }

                    if (rewindDataList != null)
                    {
                        foreach (RewindData rewindData in rewindDataList)
                        {
                            var nextRewindIndex = rewindData.PreviousBlockHash.Height + 1;

                            this.logger.LogDebug("Rewind state #{0} created.", nextRewindIndex);

                            transaction.Insert("Rewind", nextRewindIndex, this.dataStoreSerializer.Serialize(rewindData));
                        }
                    }

                    insertedEntities += unspentOutputs.Count;
                    transaction.Commit();
                }
            }

            this.performanceCounter.AddInsertedEntities(insertedEntities);
        }

        /// <summary>
        /// Creates new disposable DBreeze transaction.
        /// </summary>
        /// <returns>Transaction object.</returns>
        public DBreeze.Transactions.Transaction CreateTransaction()
        {
            return this.dBreeze.GetTransaction();
        }

        public RewindData GetRewindData(int height)
        {
            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.SynchronizeTables("BlockHash", "Coins", "Rewind");
                Row<int, byte[]> row = transaction.Select<int, byte[]>("Rewind", height);
                return row.Exists ? this.dataStoreSerializer.Deserialize<RewindData>(row.Value) : null;
            }
        }

        /// <inheritdoc />
        public HashHeightPair Rewind()
        {
            HashHeightPair res = null;
            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.SynchronizeTables("BlockHash", "Coins", "Rewind");

                transaction.ValuesLazyLoadingIsOn = false;

                HashHeightPair current = this.GetTipHash(transaction);

                Row<int, byte[]> row = transaction.Select<int, byte[]>("Rewind", current.Height);

                if (!row.Exists)
                {
                    throw new InvalidOperationException($"No rewind data found for block `{current}`");
                }

                transaction.RemoveKey("Rewind", row.Key);

                var rewindData = this.dataStoreSerializer.Deserialize<RewindData>(row.Value);

                this.SetBlockHash(transaction, rewindData.PreviousBlockHash);

                foreach (OutPoint outPoint in rewindData.OutputsToRemove)
                {
                    this.logger.LogDebug("Outputs of outpoint '{0}' will be removed.", outPoint);
                    transaction.RemoveKey("Coins", outPoint.ToBytes());
                }

                foreach (RewindDataOutput rewindDataOutput in rewindData.OutputsToRestore)
                {
                    this.logger.LogDebug("Outputs of outpoint '{0}' will be restored.", rewindDataOutput.OutPoint);
                    transaction.Insert("Coins", rewindDataOutput.OutPoint.ToBytes(), this.dataStoreSerializer.Serialize(rewindDataOutput.Coins));
                }

                res = rewindData.PreviousBlockHash;

                transaction.Commit();
            }

            return res;
        }

        /// <summary>
        /// Persists unsaved POS blocks information to the database.
        /// </summary>
        /// <param name="stakeEntries">List of POS block information to be examined and persists if unsaved.</param>
        public void PutStake(IEnumerable<StakeItem> stakeEntries)
        {
            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.SynchronizeTables("Stake");
                this.PutStakeInternal(transaction, stakeEntries);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Persists unsaved POS blocks information to the database.
        /// </summary>
        /// <param name="transaction">Open dBreeze transaction.</param>
        /// <param name="stakeEntries">List of POS block information to be examined and persists if unsaved.</param>
        private void PutStakeInternal(DBreeze.Transactions.Transaction transaction, IEnumerable<StakeItem> stakeEntries)
        {
            foreach (StakeItem stakeEntry in stakeEntries)
            {
                if (!stakeEntry.InStore)
                {
                    transaction.Insert("Stake", stakeEntry.BlockId.ToBytes(false), this.dataStoreSerializer.Serialize(stakeEntry.BlockStake));
                    stakeEntry.InStore = true;
                }
            }
        }

        /// <summary>
        /// Retrieves POS blocks information from the database.
        /// </summary>
        /// <param name="blocklist">List of partially initialized POS block information that is to be fully initialized with the values from the database.</param>
        public void GetStake(IEnumerable<StakeItem> blocklist)
        {
            using (DBreeze.Transactions.Transaction transaction = this.CreateTransaction())
            {
                transaction.SynchronizeTables("Stake");
                transaction.ValuesLazyLoadingIsOn = false;

                foreach (StakeItem blockStake in blocklist)
                {
                    this.logger.LogDebug("Loading POS block hash '{0}' from the database.", blockStake.BlockId);
                    Row<byte[], byte[]> stakeRow = transaction.Select<byte[], byte[]>("Stake", blockStake.BlockId.ToBytes(false));

                    if (stakeRow.Exists)
                    {
                        blockStake.BlockStake = this.dataStoreSerializer.Deserialize<BlockStake>(stakeRow.Value);
                        blockStake.InStore = true;
                    }
                }
            }
        }

        private void AddBenchStats(StringBuilder log)
        {
            log.AppendLine("======DBreezeCoinView Bench======");

            BackendPerformanceSnapshot snapShot = this.performanceCounter.Snapshot();

            if (this.latestPerformanceSnapShot == null)
                log.AppendLine(snapShot.ToString());
            else
                log.AppendLine((snapShot - this.latestPerformanceSnapShot).ToString());

            this.latestPerformanceSnapShot = snapShot;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.dBreeze.Dispose();
        }
    }
}
