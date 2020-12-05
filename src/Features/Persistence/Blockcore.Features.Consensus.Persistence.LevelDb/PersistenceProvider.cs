using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Interfaces;
using Blockcore.Persistence;
using Blockcore.Persistence.LevelDb;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Consensus.Persistence.LevelDb
{
    public class PersistenceProvider : PersistenceProviderBase<ConsensusFeature>
    {
        public override string Tag => LevelDbPersistence.Name;

        public override void AddRequiredServices(IServiceCollection services)
        {
            services.AddSingleton<ICoindb, LevelDbCoindb>();
            services.AddSingleton<IProvenBlockHeaderRepository, LevelDbProvenBlockHeaderRepository>();
        }
    }
}