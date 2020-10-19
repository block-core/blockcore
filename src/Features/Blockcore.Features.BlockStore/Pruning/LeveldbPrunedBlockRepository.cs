using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Networks;
using Blockcore.Utilities;
using DBreeze.DataTypes;
using LevelDB;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Pruning

{
    /// <inheritdoc />
    public class LeveldbPrunedBlockRepository : IPrunedBlockRepository

    {
        private readonly IBlockRepository blockRepository;
        private readonly DataStoreSerializer dataStoreSerializer;
        private readonly ILogger logger;
        private static readonly byte[] prunedTipKey = new byte[2];
        private readonly StoreSettings storeSettings;
        private readonly Network network;

        /// <inheritdoc />
        public HashHeightPair PrunedTip { get; private set; }

        public LeveldbPrunedBlockRepository(IBlockRepository blockRepository, DataStoreSerializer dataStoreSerializer, ILoggerFactory loggerFactory, StoreSettings storeSettings, Network network)
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
            this.LoadPrunedTip((DB)this.blockRepository.DbInstance);
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
                    ((DB)this.blockRepository.DbInstance).Put(DBH.Key(RocksdbBlockRepository.CommonTableName, prunedTipKey), this.dataStoreSerializer.Serialize(this.PrunedTip));
                }
            }

            return;
        }

        private void LoadPrunedTip(DB rocksdb)
        {
            if (this.PrunedTip == null)
            {
                lock (this.blockRepository.Locker)

                {
                    byte[] row = rocksdb.Get(DBH.Key(RocksdbBlockRepository.CommonTableName, prunedTipKey));
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
                ((DB)this.blockRepository.DbInstance).Put(DBH.Key(RocksdbBlockRepository.CommonTableName, prunedTipKey), this.dataStoreSerializer.Serialize(this.PrunedTip));
            }
        }
    }
}