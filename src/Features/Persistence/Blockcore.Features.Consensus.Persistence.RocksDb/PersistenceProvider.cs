using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Interfaces;
using Blockcore.Persistence;
using Blockcore.Persistence.RocksDb;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Consensus.Persistence.RocksDb
{
    public class PowPersistenceProvider : PersistenceProviderBase<PowConsensusFeature>
    {
        public override string Tag => RocksDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<ICoindb, RocksDbCoindb>();
        }
    }

    public class PosPersistenceProvider : PersistenceProviderBase<PosConsensusFeature>
    {
        public override string Tag => RocksDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<ICoindb, RocksDbCoindb>();
            services.AddSingleton<IProvenBlockHeaderRepository, RocksDbProvenBlockHeaderRepository>();
        }
    }
}