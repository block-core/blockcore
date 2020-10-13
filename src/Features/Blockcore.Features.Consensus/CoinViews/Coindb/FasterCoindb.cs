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
using FASTER.core;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Crypto;

namespace Blockcore.Features.Consensus.CoinViews.Coindb
{
    public class FasterCoindb : ICoindb, IStakdb, IDisposable
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

        private DataStoreSerializer dataStoreSerializer;

        private string dataFolder;

        public FasterKV<Types.StoreKey, Types.StoreValue, Types.StoreInput, Types.StoreOutput, Types.StoreContext, Types.StoreFunctions> db;
        public IDevice log;
        public IDevice objLog;

        public FasterCoindb(Network network, DataFolder dataFolder, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory, INodeStats nodeStats, DataStoreSerializer dataStoreSerializer)
             : this(network, dataFolder.CoindbPath, dateTimeProvider, loggerFactory, nodeStats, dataStoreSerializer)
        {
        }

        public FasterCoindb(Network network, string folder, IDateTimeProvider dateTimeProvider, ILoggerFactory loggerFactory, INodeStats nodeStats, DataStoreSerializer dataStoreSerializer)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotEmpty(folder, nameof(folder));

            this.dataStoreSerializer = dataStoreSerializer;

            // Create the coinview folder if it does not exist.
            Directory.CreateDirectory(folder);

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.network = network;
            this.performanceCounter = new BackendPerformanceCounter(dateTimeProvider);
            this.dataFolder = folder;

            nodeStats.RegisterStats(this.AddBenchStats, StatsType.Benchmark, this.GetType().Name, 400);
        }

        public void Initialize()
        {
            var logSize = 1L << 20;
            this.log = Devices.CreateLogDevice(@$"{this.dataFolder}\Store-hlog.log", preallocateFile: false);
            this.objLog = Devices.CreateLogDevice(@$"{this.dataFolder}\Store-hlog-obj.log", preallocateFile: false);

            var logSettings = new LogSettings
            {
                LogDevice = this.log,
                ObjectLogDevice = this.objLog,
                MutableFraction = 0.9,
                PageSizeBits = 25, // 25
                SegmentSizeBits = 30, // 30
                MemorySizeBits = 34 // 34
            };

            var chekcpoint = new CheckpointSettings
            {
                CheckpointDir = $"{this.dataFolder}/checkpoints"
            };

            var serializer = new SerializerSettings<Types.StoreKey, Types.StoreValue>
            {
                keySerializer = () => new Types.StoreKeySerializer(),
                valueSerializer = () => new Types.StoreValueSerializer()
            };

            this.db = new FasterKV
                <Types.StoreKey, Types.StoreValue, Types.StoreInput, Types.StoreOutput, Types.StoreContext, Types.StoreFunctions>(
                    logSize,
                    new Types.StoreFunctions(),
                    logSettings,
                    chekcpoint,
                    serializer);

            if (Directory.Exists($"{this.dataFolder}/checkpoints"))
            {
                this.db.Recover();
            }

            Block genesis = this.network.GetGenesis();

            using (var session = this.db.NewSession())
            {
                var wrapper = new Types.SessionWrapper { Session = session };
                if (this.GetTipHash() == null)
                {
                    this.SetBlockHash(wrapper, new HashHeightPair(genesis.GetHash(), 0));

                    this.Checkpoint();
                    // Genesis coin is unspendable so do not add the coins.
                    //   transaction.Commit();
                }
            }
        }

        public Guid Checkpoint()
        {
            Guid token = default(Guid);

            this.db.TakeFullCheckpoint(out token);
            this.db.CompleteCheckpointAsync().GetAwaiter().GetResult();

            var indexCheckpointDir = new DirectoryInfo($"{this.dataFolder}/checkpoints/cpr-checkpoints");

            int counter = 0;
            foreach (DirectoryInfo info in indexCheckpointDir.GetDirectories().OrderByDescending(f => f.LastWriteTime))
            {
                if (info.Name == token.ToString())
                    continue;

                if (++counter < 5)
                    continue;

                Directory.Delete(info.FullName, true);
            }

            var hlogCheckpointDir = new DirectoryInfo($"{this.dataFolder}/checkpoints/index-checkpoints");

            counter = 0;
            foreach (DirectoryInfo info in hlogCheckpointDir.GetDirectories().OrderByDescending(f => f.LastWriteTime))
            {
                if (info.Name == token.ToString())
                    continue;

                if (++counter < 5)
                    continue;

                Directory.Delete(info.FullName, true);
            }

            return token;
        }

        public void Dispose()
        {
            this.db.Dispose();
            this.log.Close();
            this.objLog.Close();
        }

        public HashHeightPair GetTipHash()
        {
            HashHeightPair tipHash;

            var session = this.db.NewSession();

            using (session)
            {
                tipHash = this.GetTipHash(new Types.SessionWrapper { Session = session });
            }

            return tipHash;
        }

        private HashHeightPair GetTipHash(Types.SessionWrapper session)
        {
            if (this.blockHash == null)
            {
                Types.StoreInput input = new Types.StoreInput();
                Types.StoreOutput output = new Types.StoreOutput();
                var lastblockKey = new Types.StoreKey { tableType = "BlockHash", key = blockHashKey };
                Types.StoreContext context = new Types.StoreContext();
                var blkStatus = session.Session.Read(ref lastblockKey, ref input, ref output, context, 1); // TODO: use height a serial number?

                if (blkStatus == Status.OK)
                {
                    this.blockHash = new HashHeightPair();
                    this.blockHash.FromBytes(output.value.value);
                }
            }

            return this.blockHash;
        }

        private void SetBlockHash(Types.SessionWrapper session, HashHeightPair nextBlockHash)
        {
            this.blockHash = nextBlockHash;

            var lastblockKey = new Types.StoreKey { tableType = "BlockHash", key = blockHashKey };
            var lastBlockvalue = new Types.StoreValue { value = nextBlockHash.ToBytes() };
            Types.StoreContext context = new Types.StoreContext();
            var blkStatus = session.Session.Upsert(ref lastblockKey, ref lastBlockvalue, context, 1); // TODO: use height a serial number?
            if (blkStatus != Status.OK)
            {
                throw new Exception();
            }
        }

        public FetchCoinsResponse FetchCoins(OutPoint[] utxos)
        {
            FetchCoinsResponse res = new FetchCoinsResponse();
            using (var session = this.db.NewSession())
            {
                using (new StopwatchDisposable(o => this.performanceCounter.AddQueryTime(o)))
                {
                    this.performanceCounter.AddQueriedEntities(utxos.Length);

                    Types.StoreInput input = new Types.StoreInput();
                    Types.StoreOutput output = new Types.StoreOutput();
                    Types.StoreContext context = new Types.StoreContext();
                    var readKey = new Types.StoreKey { tableType = "Coins" };

                    foreach (OutPoint outPoint in utxos)
                    {
                        output.value = null;
                        readKey.key = outPoint.ToBytes();
                        var addStatus = session.Read(ref readKey, ref input, ref output, context, 1);

                        if (addStatus == Status.PENDING)
                        {
                            session.CompletePending(true);
                            context.FinalizeRead(ref addStatus, ref output);
                        }

                        Coins outputs = addStatus == Status.OK ? this.dataStoreSerializer.Deserialize<Coins>(output.value.value) : null;

                        this.logger.LogDebug("Outputs for '{0}' were {1}.", outPoint, outputs == null ? "NOT loaded" : "loaded");

                        res.UnspentOutputs.Add(outPoint, new UnspentOutput(outPoint, outputs));
                    }
                }
            }

            return res;
        }

        public void SaveChanges(IList<UnspentOutput> unspentOutputs, HashHeightPair oldBlockHash, HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null)
        {
            int insertedEntities = 0;

            using (var session = this.db.NewSession())
            {
                using (new StopwatchDisposable(o => this.performanceCounter.AddInsertTime(o)))
                {
                    var wrapper = new Types.SessionWrapper { Session = session };
                    HashHeightPair current = this.GetTipHash(wrapper);
                    if (current != oldBlockHash)
                    {
                        this.logger.LogTrace("(-)[BLOCKHASH_MISMATCH]");
                        throw new InvalidOperationException("Invalid oldBlockHash");
                    }

                    this.SetBlockHash(wrapper, nextBlockHash);

                    // Here we'll add items to be inserted in a second pass.
                    List<UnspentOutput> toInsert = new List<UnspentOutput>();

                    foreach (var coin in unspentOutputs.OrderBy(utxo => utxo.OutPoint, new OutPointComparer()))
                    {
                        if (coin.Coins == null)
                        {
                            this.logger.LogDebug("Outputs of transaction ID '{0}' are prunable and will be removed from the database.", coin.OutPoint);

                            var deteletKey = new Types.StoreKey { tableType = "Coins", key = coin.OutPoint.ToBytes() };
                            Types.StoreContext context = new Types.StoreContext();
                            var deleteStatus = session.Delete(ref deteletKey, context, 1);

                            if (deleteStatus != Status.OK)
                                throw new Exception();
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

                        var upsertKey = new Types.StoreKey { tableType = "Coins", key = coin.OutPoint.ToBytes() };
                        var upsertValue = new Types.StoreValue { value = this.dataStoreSerializer.Serialize(coin.Coins) };
                        Types.StoreContext context2 = new Types.StoreContext();
                        var addStatus = session.Upsert(ref upsertKey, ref upsertValue, context2, 1);

                        if (addStatus != Status.OK)
                            throw new Exception();
                    }

                    if (rewindDataList != null)
                    {
                        foreach (RewindData rewindData in rewindDataList)
                        {
                            var nextRewindIndex = rewindData.PreviousBlockHash.Height + 1;

                            this.logger.LogDebug("Rewind state #{0} created.", nextRewindIndex);

                            var upsertKey = new Types.StoreKey { tableType = "Rewind", key = BitConverter.GetBytes(nextRewindIndex) };
                            var upsertValue = new Types.StoreValue { value = this.dataStoreSerializer.Serialize(rewindData) };
                            Types.StoreContext context2 = new Types.StoreContext();
                            var addStatus = session.Upsert(ref upsertKey, ref upsertValue, context2, 1);

                            if (addStatus != Status.OK)
                                throw new Exception();
                        }
                    }

                    insertedEntities += unspentOutputs.Count;
                    this.Checkpoint();
                }
            }

            this.performanceCounter.AddInsertedEntities(insertedEntities);
        }

        public HashHeightPair Rewind()
        {
            HashHeightPair res = null;
            using (var session = this.db.NewSession())
            {
                var wrapper = new Types.SessionWrapper { Session = session };

                HashHeightPair current = this.GetTipHash(wrapper);

                Types.StoreInput input1 = new Types.StoreInput();
                Types.StoreOutput output1 = new Types.StoreOutput();
                Types.StoreContext context = new Types.StoreContext();
                var readKey = new Types.StoreKey { tableType = "Rewind", key = BitConverter.GetBytes(current.Height) };
                var addStatus = session.Read(ref readKey, ref input1, ref output1, context, 1);

                if (addStatus == Status.PENDING)
                {
                    session.CompletePending(true);
                    context.FinalizeRead(ref addStatus, ref output1);
                }

                if (addStatus != Status.OK)
                {
                    throw new InvalidOperationException($"No rewind data found for block `{current}`");
                }

                var deteletKey = new Types.StoreKey { tableType = "Rewind", key = BitConverter.GetBytes(current.Height) };
                Types.StoreContext contextDel = new Types.StoreContext();
                var deleteStatus = session.Delete(ref readKey, contextDel, 1);

                if (deleteStatus != Status.OK)
                    throw new Exception();

                var rewindData = this.dataStoreSerializer.Deserialize<RewindData>(output1.value.value);

                this.SetBlockHash(wrapper, rewindData.PreviousBlockHash);

                foreach (OutPoint outPoint in rewindData.OutputsToRemove)
                {
                    this.logger.LogDebug("Outputs of outpoint '{0}' will be removed.", outPoint);
                    deteletKey = new Types.StoreKey { tableType = "Coins", key = outPoint.ToBytes() };
                    contextDel = new Types.StoreContext();
                    deleteStatus = session.Delete(ref readKey, contextDel, 1);

                    if (deleteStatus != Status.OK)
                        throw new Exception();
                }

                foreach (RewindDataOutput rewindDataOutput in rewindData.OutputsToRestore)
                {
                    this.logger.LogDebug("Outputs of outpoint '{0}' will be restored.", rewindDataOutput.OutPoint);

                    var upsertKey = new Types.StoreKey { tableType = "Coins", key = rewindDataOutput.OutPoint.ToBytes() };
                    var upsertValue = new Types.StoreValue { value = this.dataStoreSerializer.Serialize(rewindDataOutput.Coins) };
                    Types.StoreContext context2 = new Types.StoreContext();
                    addStatus = session.Upsert(ref upsertKey, ref upsertValue, context2, 1);

                    if (addStatus != Status.OK)
                        throw new Exception();
                }

                res = rewindData.PreviousBlockHash;
            }

            return res;
        }

        private void AddBenchStats(StringBuilder log)
        {
            log.AppendLine("======Faster Bench======");

            BackendPerformanceSnapshot snapShot = this.performanceCounter.Snapshot();

            if (this.latestPerformanceSnapShot == null)
                log.AppendLine(snapShot.ToString());
            else
                log.AppendLine((snapShot - this.latestPerformanceSnapShot).ToString());

            this.latestPerformanceSnapShot = snapShot;
        }

        public RewindData GetRewindData(int height)
        {
            throw new NotImplementedException();
        }

        public void PutStake(IEnumerable<StakeItem> stakeEntries)
        {
            throw new NotImplementedException();
        }

        private void PutStakeInternal(DBreeze.Transactions.Transaction transaction, IEnumerable<StakeItem> stakeEntries)
        {
            throw new NotImplementedException();
        }

        public void GetStake(IEnumerable<StakeItem> blocklist)
        {
            throw new NotImplementedException();
        }

        public class Types
        {
            public class SessionWrapper
            {
                public ClientSession<Types.StoreKey, Types.StoreValue, Types.StoreInput, Types.StoreOutput, Types.StoreContext, Types.StoreFunctions> Session { get; set; }
            }

            public class StoreKey : IFasterEqualityComparer<StoreKey>
            {
                public byte[] key;
                public string tableType;

                public virtual long GetHashCode64(ref StoreKey key)
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(key.tableType);
                    byte[] b = bytes.ToArray().Concat(key.key).ToArray();

                    var hash256 = Hashes.Hash256(b);

                    long res = 0;
                    foreach (byte bt in hash256.ToBytes())
                        res = res * 31 * 31 * bt + 17;

                    return res;
                }

                public virtual bool Equals(ref StoreKey k1, ref StoreKey k2)
                {
                    return k1.key.SequenceEqual(k2.key) && k1.tableType == k2.tableType;
                }
            }

            public class StoreKeySerializer : BinaryObjectSerializer<StoreKey>
            {
                public override void Deserialize(ref StoreKey obj)
                {
                    var bytesr = new byte[4];
                    this.reader.Read(bytesr, 0, 4);
                    var sizet = BitConverter.ToInt32(bytesr);
                    var bytes = new byte[sizet];
                    this.reader.Read(bytes, 0, sizet);
                    obj.tableType = Encoding.UTF8.GetString(bytes);

                    bytesr = new byte[4];
                    this.reader.Read(bytesr, 0, 4);
                    var size = BitConverter.ToInt32(bytesr);
                    obj.key = new byte[size];
                    this.reader.Read(obj.key, 0, size);
                }

                public override void Serialize(ref StoreKey obj)
                {
                    var bytes = Encoding.UTF8.GetBytes(obj.tableType);
                    var len = BitConverter.GetBytes(bytes.Length);
                    this.writer.Write(len);
                    this.writer.Write(bytes);

                    len = BitConverter.GetBytes(obj.key.Length);
                    this.writer.Write(len);
                    this.writer.Write(obj.key);
                }
            }

            public class StoreValue
            {
                public byte[] value;

                public StoreValue()
                {
                }
            }

            public class StoreValueSerializer : BinaryObjectSerializer<StoreValue>
            {
                public override void Deserialize(ref StoreValue obj)
                {
                    var bytesr = new byte[4];
                    this.reader.Read(bytesr, 0, 4);
                    int size = BitConverter.ToInt32(bytesr);
                    obj.value = this.reader.ReadBytes(size);
                }

                public override void Serialize(ref StoreValue obj)
                {
                    var len = BitConverter.GetBytes(obj.value.Length);
                    this.writer.Write(len);
                    this.writer.Write(obj.value);
                }
            }

            public class StoreInput
            {
                public byte[] value;
            }

            public class StoreOutput
            {
                public StoreValue value;
            }

            public class StoreContext
            {
                private Status status;
                private StoreOutput output;

                internal void Populate(ref Status status, ref StoreOutput output)
                {
                    this.status = status;
                    this.output = output;
                }

                internal void FinalizeRead(ref Status status, ref StoreOutput output)
                {
                    status = this.status;
                    output = this.output;
                }
            }

            public class StoreFunctions : IFunctions<StoreKey, StoreValue, StoreInput, StoreOutput, StoreContext>
            {
                public void RMWCompletionCallback(ref StoreKey key, ref StoreInput input, StoreContext ctx, Status status)
                {
                }

                public void ReadCompletionCallback(ref StoreKey key, ref StoreInput input, ref StoreOutput output, StoreContext ctx, Status status)
                {
                    ctx.Populate(ref status, ref output);
                }

                public void UpsertCompletionCallback(ref StoreKey key, ref StoreValue value, StoreContext ctx)
                {
                }

                public void DeleteCompletionCallback(ref StoreKey key, StoreContext ctx)
                {
                }

                public void CopyUpdater(ref StoreKey key, ref StoreInput input, ref StoreValue oldValue, ref StoreValue newValue)
                {
                }

                public void InitialUpdater(ref StoreKey key, ref StoreInput input, ref StoreValue value)
                {
                }

                public bool InPlaceUpdater(ref StoreKey key, ref StoreInput input, ref StoreValue value)
                {
                    if (value.value.Length < input.value.Length)
                        return false;

                    value.value = input.value;
                    return true;
                }

                public void SingleReader(ref StoreKey key, ref StoreInput input, ref StoreValue value, ref StoreOutput dst)
                {
                    dst.value = value;
                }

                public void ConcurrentReader(ref StoreKey key, ref StoreInput input, ref StoreValue value, ref StoreOutput dst)
                {
                    dst.value = value;
                }

                public bool ConcurrentWriter(ref StoreKey key, ref StoreValue src, ref StoreValue dst)
                {
                    if (src == null)
                        return false;

                    if (dst.value.Length != src.value.Length)
                        return false;

                    dst = src;
                    return true;
                }

                public void CheckpointCompletionCallback(string sessionId, CommitPoint commitPoint)
                {
                }

                public void SingleWriter(ref StoreKey key, ref StoreValue src, ref StoreValue dst)
                {
                    dst = src;
                }
            }
        }
    }
}