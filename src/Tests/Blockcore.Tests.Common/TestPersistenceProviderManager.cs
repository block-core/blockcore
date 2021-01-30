using Blockcore.Configuration;
using Blockcore.Persistence;

namespace Blockcore.Tests.Common
{
    /// <summary>
    /// To be used when no test has to use persistence.
    /// Doesn't register anything.
    /// </summary>
    /// <seealso cref="Utilities.Store.IPersistenceProviderManager" />
    public class TestPersistenceProviderManager : PersistenceProviderManager
    {
        public TestPersistenceProviderManager(NodeSettings nodeSettings) : base(nodeSettings)
        {
        }

        public override void Initialize()
        {
            if (this.nodeSettings != null && this.nodeSettings.Network.Consensus.IsProofOfStake)
            {
                // manually register LevelDb implementation
                this.persistenceProviders["LevelDb".ToLowerInvariant()] = new System.Collections.Generic.List<IPersistenceProvider>
                {
                    new Features.Base.Persistence.LevelDb.PersistenceProvider(),
                    new Features.Consensus.Persistence.LevelDb.PosPersistenceProvider(),
                    new Features.BlockStore.Persistence.LevelDb.PersistenceProvider(),
                };
            }
            else
            {
                // manually register LevelDb implementation
                this.persistenceProviders["LevelDb".ToLowerInvariant()] = new System.Collections.Generic.List<IPersistenceProvider>
                {
                    new Features.Base.Persistence.LevelDb.PersistenceProvider(),
                    new Features.Consensus.Persistence.LevelDb.PowPersistenceProvider(),
                    new Features.BlockStore.Persistence.LevelDb.PersistenceProvider(),
                };
            }
        }
    }
}