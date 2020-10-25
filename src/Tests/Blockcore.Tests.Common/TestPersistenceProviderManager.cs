using Blockcore.Configuration;
using Blockcore.Features.Persistence.LevelDb;
using Blockcore.Utilities.Store;

namespace Blockcore.Tests.Common
{
    /// <summary>
    /// To be used when no test has to use persistence.
    /// Doesn't register anything.
    /// </summary>
    /// <seealso cref="Blockcore.Utilities.Store.IPersistenceProviderManager" />
    public class TestPersistenceProviderManager : PersistenceProviderManager
    {
        public TestPersistenceProviderManager(NodeSettings nodeSettings) : base(nodeSettings ?? NodeSettings.Default(KnownNetworks.Main), new LevelDbPersistenceProvider())
        {
        }
    }
}
