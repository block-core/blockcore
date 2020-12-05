using System;
using Blockcore.Base;
using Blockcore.Builder.Feature;
using Blockcore.Consensus.Chain;
using Blockcore.Features.BlockStore;
using Blockcore.Features.BlockStore.Pruning;
using Blockcore.Features.BlockStore.Repository;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Interfaces;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Persistence.Rocksdb
{
    public class RocksDbPersistenceProvider : IPersistenceProvider
    {
        const string NAME = "Rocksdb";

        public string Name => NAME;

        public void AddRequiredServices<TFeature>(IServiceCollection services) where TFeature : IFullNodeFeature
        {
            Type type = typeof(TFeature);

            switch (type)
            {
                case Type _ when type == typeof(BaseFeature):
                    services.AddSingleton<IChainStore, RocksDbChainStore>();
                    services.AddSingleton<IKeyValueRepository, RocksDbKeyValueRepository>();
                    break;

                case Type _ when type == typeof(ConsensusFeature):
                    services.AddSingleton<ICoindb, RocksDbCoindb>();
                    services.AddSingleton<IProvenBlockHeaderRepository, RocksDbProvenBlockHeaderRepository>();
                    break;

                case Type _ when type == typeof(BlockStoreFeature):
                    services.AddSingleton<IBlockRepository, RocksdbBlockRepository>();
                    services.AddSingleton<IPrunedBlockRepository, RocksDbPrunedBlockRepository>();
                    break;
            }
        }
    }
}
