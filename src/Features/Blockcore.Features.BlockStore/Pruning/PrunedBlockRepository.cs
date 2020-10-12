using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Utilities;
using DBreeze.DataTypes;
using LevelDB;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Pruning
{
    /// <inheritdoc />
    public class PrunedBlockRepository : IPrunedBlockRepository
    {
        private readonly IBlockRepository blockRepository;
        private readonly DataStoreSerializer dataStoreSerializer;
        private readonly ILogger logger;
        private static readonly byte[] prunedTipKey = new byte[2];
        private readonly StoreSettings storeSettings;
        private readonly Network network;

        /// <inheritdoc />
        public HashHeightPair PrunedTip { get; private set; }

        public PrunedBlockRepository(IBlockRepository blockRepository, DataStoreSerializer dataStoreSerializer, ILoggerFactory loggerFactory, StoreSettings storeSettings, Network network)
        {
            this.blockRepository = blockRepository;
            this.dataStoreSerializer = dataStoreSerializer;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.storeSettings = storeSettings;
            this.network = network;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            this.LoadPrunedTip(this.blockRepository.RocksDb);
        }

        /// <inheritdoc />
        public void PrepareDatabase()
        {
            if (this.PrunedTip == null)
            {
                Block genesis = this.network.GetGenesis();

                this.PrunedTip = new HashHeightPair(genesis.GetHash(), 0);

                lock (this.blockRepository.Locker)
                {
                    this.blockRepository.RocksDb.Put(DBH.Key(BlockRepository.CommonTableName, prunedTipKey), this.dataStoreSerializer.Serialize(this.PrunedTip));
                }
            }

            return;
        }

        private void LoadPrunedTip(RocksDbSharp.RocksDb rocksdb)
        {
            if (this.PrunedTip == null)
            {
                lock (this.blockRepository.Locker)
                {
                    byte[] row = rocksdb.Get(DBH.Key(BlockRepository.CommonTableName, prunedTipKey));
                    if (row != null)
                    {
                        this.PrunedTip = this.dataStoreSerializer.Deserialize<HashHeightPair>(row);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void UpdatePrunedTip(ChainedHeader tip)
        {
            this.PrunedTip = new HashHeightPair(tip);

            lock (this.blockRepository.Locker)
            {
                this.blockRepository.RocksDb.Put(DBH.Key(BlockRepository.CommonTableName, prunedTipKey), this.dataStoreSerializer.Serialize(this.PrunedTip));
            }
        }
    }
}