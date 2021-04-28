using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Interfaces;
using Blockcore.Persistence;
using Blockcore.Persistence.LevelDb;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Consensus.Persistence.LevelDb
{
    public class PowPersistenceProvider : PersistenceProviderBase<PowConsensusFeature>
    {
        public override string Tag => LevelDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<ICoindb, LevelDbCoindb>();
        }
    }

    public class PosPersistenceProvider : PersistenceProviderBase<PosConsensusFeature>
    {
        public override string Tag => LevelDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<ICoindb, LevelDbCoindb>();
            services.AddSingleton<IProvenBlockHeaderRepository, LevelDbProvenBlockHeaderRepository>();
        }
    }
}