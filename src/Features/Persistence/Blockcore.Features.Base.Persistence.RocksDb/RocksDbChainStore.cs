using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Networks;
using Blockcore.Utilities;
using NBitcoin;
using RocksDbSharp;
using DB = RocksDbSharp.RocksDb;

namespace Blockcore.Features.Base.Persistence.RocksDb
{
    /// <summary>
    /// Rocksdb implementation of the chain storage
    /// </summary>
    /// <seealso cref="Blockcore.Consensus.Chain.IChainStore" />
    /// <seealso cref="System.IDisposable" />
    public class RocksDbChainStore : IChainStore, IDisposable
    {
        private readonly Network network;

        internal static readonly byte ChainTableName = 1;
        internal static readonly byte HeaderTableName = 2;

        /// <summary>
        /// Headers that are close to the tip
        /// </summary>
        private readonly MemoryCountCache<uint256, BlockHeader> nearTipHeaders;

        /// <summary>
        /// Headers that are close to the tip
        /// </summary>
        private readonly MemoryCountCache<uint256, BlockHeader> recentHeaders;

        private readonly DB rocksdb;

        private object locker;

        public RocksDbChainStore(Network network, DataFolder dataFolder, ChainIndexer chainIndexer)
        {
            this.network = network;
            this.ChainIndexer = chainIndexer;
            this.nearTipHeaders = new MemoryCountCache<uint256, BlockHeader>(601);
            this.recentHeaders = new MemoryCountCache<uint256, BlockHeader>(100);
            this.locker = new object();

            // Open a connection to a new DB and create if not found
            var options = new DbOptions().SetCreateIfMissing(true);
            this.rocksdb = DB.Open(options, dataFolder.ChainPath);
        }

        public ChainIndexer ChainIndexer { get; }

        public BlockHeader GetHeader(ChainedHeader chainedHeader, uint256 hash)
        {
            if (this.nearTipHeaders.TryGetValue(hash, out BlockHeader blockHeader))
            {
                return blockHeader;
            }

            if (this.recentHeaders.TryGetValue(hash, out blockHeader))
            {
                return blockHeader;
            }

            ReadOnlySpan<byte> bytes = hash.ToReadOnlySpan();

            lock (this.locker)
            {
                bytes = this.rocksdb.Get(DBH.Key(HeaderTableName, bytes));
            }

            if (bytes == null)
            {
                throw new ApplicationException("Header must exist if requested");
            }

            blockHeader = this.network.Consensus.ConsensusFactory.CreateBlockHeader();
            blockHeader.FromBytes(bytes.ToArray(), this.network.Consensus.ConsensusFactory);

            // If the header is 500 blocks behind tip or 100 blocks ahead cache it.
            if ((chainedHeader.Height > this.ChainIndexer.Height - 500) && (chainedHeader.Height <= this.ChainIndexer.Height + 100))
            {
                this.nearTipHeaders.AddOrUpdate(hash, blockHeader);
            }
            else
            {
                this.recentHeaders.AddOrUpdate(hash, blockHeader);
            }

            return blockHeader;
        }

        public bool PutHeader(BlockHeader blockHeader)
        {
            ConsensusFactory consensusFactory = this.network.Consensus.ConsensusFactory;

            lock (this.locker)
            {
                this.rocksdb.Put(DBH.Key(HeaderTableName, blockHeader.GetHash().ToReadOnlySpan()), blockHeader.ToBytes(consensusFactory));
            }

            return true;
        }

        public IEnumerable<ChainData> GetChainData()
        {
            Dictionary<int, ChainData> list = new Dictionary<int, ChainData>();

            lock (this.locker)
            {
                using (var iterator = this.rocksdb.NewIterator())
                {
                    iterator.SeekToFirst();

                    while (iterator.Valid() && iterator.Key()[0] == ChainTableName)
                    {
                        var height = BitConverter.ToInt32(iterator.Key().AsSpan(1));
                        byte[] bytes = iterator.Value();

                        var data = new ChainData();
                        data.FromBytes(bytes, this.network.Consensus.ConsensusFactory);
                        list.Add(height, data);

                        iterator.Next();
                    }
                }
            }

            // Order by height and return new array with ChainData.
            return list.OrderBy(c => c.Key).Select(c => c.Value);
        }

        public ChainData GetChainData(int height)
        {
            byte[] bytes = null;

            lock (this.locker)
            {
                bytes = this.rocksdb.Get(DBH.Key(ChainTableName, BitConverter.GetBytes(height)));
            }

            if (bytes == null)
            {
                return null;
            }

            var data = new ChainData();
            data.FromBytes(bytes, this.network.Consensus.ConsensusFactory);

            return data;
        }

        public void PutChainData(IEnumerable<ChainDataItem> items)
        {
            using (var batch = new WriteBatch())
            {
                foreach (var item in items)
                {
                    batch.Put(DBH.Key(ChainTableName, BitConverter.GetBytes(item.Height)), item.Data.ToBytes(this.network.Consensus.ConsensusFactory));
                }

                lock (this.locker)
                {
                    this.rocksdb.Write(batch);
                }
            }
        }

        public void Dispose()
        {
            this.rocksdb?.Dispose();
        }
    }
}