using Blockcore.Features.BlockStore.Pruning;
using Blockcore.Features.BlockStore.Repository;
using Blockcore.Persistence;
using Blockcore.Persistence.RocksDb;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.BlockStore.Persistence.RocksDb
{
    public class PersistenceProvider : PersistenceProviderBase<BlockStoreFeature>
    {
        public override string Tag => RocksDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<IBlockRepository, RocksdbBlockRepository>();
            services.AddSingleton<IPrunedBlockRepository, RocksDbPrunedBlockRepository>();
        }
    }
}