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

namespace Blockcore.Features.Persistence.LevelDb
{
    public class LevelDbPersistenceProvider : IPersistenceProvider
    {
        const string NAME = "LevelDb";

        public string Name => NAME;

        public void AddRequiredServices<TFeature>(IServiceCollection services) where TFeature : IFullNodeFeature
        {
            Type type = typeof(TFeature);

            switch (type)
            {
                case Type _ when type == typeof(BaseFeature):
                    services.AddSingleton<IChainStore, LevelDbChainStore>();
                    services.AddSingleton<IKeyValueRepository, LevelDbKeyValueRepository>();
                    break;

                case Type _ when type == typeof(ConsensusFeature):
                    services.AddSingleton<ICoindb, LevelDbCoindb>();
                    services.AddSingleton<IProvenBlockHeaderRepository, LevelDbProvenBlockHeaderRepository>();
                    break;

                case Type _ when type == typeof(BlockStoreFeature):
                    services.AddSingleton<IBlockRepository, LevelDbBlockRepository>();
                    services.AddSingleton<IPrunedBlockRepository, LevelDbPrunedBlockRepository>();
                    break;
            }
        }
    }
}
