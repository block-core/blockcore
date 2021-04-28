using Blockcore.Base;
using Blockcore.Consensus.Chain;
using Blockcore.Persistence;
using Blockcore.Persistence.RocksDb;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Base.Persistence.RocksDb
{
    public class PersistenceProvider : PersistenceProviderBase<BaseFeature>
    {
        public override string Tag => RocksDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<IChainStore, RocksDbChainStore>();
            services.AddSingleton<IKeyValueRepository, RocksDbKeyValueRepository>();
        }
    }
}