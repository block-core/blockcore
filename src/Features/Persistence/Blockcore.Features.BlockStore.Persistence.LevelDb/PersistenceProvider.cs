using Blockcore.Features.BlockStore.Pruning;
using Blockcore.Features.BlockStore.Repository;
using Blockcore.Persistence;
using Blockcore.Persistence.LevelDb;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.BlockStore.Persistence.LevelDb
{
    public class PersistenceProvider : PersistenceProviderBase<BlockStoreFeature>
    {
        public override string Tag => LevelDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<IBlockRepository, LevelDbBlockRepository>();
            services.AddSingleton<IPrunedBlockRepository, LevelDbPrunedBlockRepository>();
        }
    }
}