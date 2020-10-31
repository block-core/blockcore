using Blockcore.Base;
using Blockcore.Consensus.Chain;
using Blockcore.Persistence;
using Blockcore.Persistence.LevelDb;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Base.Persistence.LevelDb
{
    public class PersistenceProvider : PersistenceProviderBase<BaseFeature>
    {
        public override string Tag => LevelDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<IChainStore, LevelDbChainStore>();
            services.AddSingleton<IKeyValueRepository, LevelDbKeyValueRepository>();
        }
    }
}