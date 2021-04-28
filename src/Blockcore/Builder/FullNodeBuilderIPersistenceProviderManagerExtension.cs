using Blockcore.Persistence;

namespace Blockcore.Builder
{
    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderIPersistenceProviderManagerExtension
    {
        /// <summary>
        /// Makes the full node builder use specific node settings.
        /// </summary>
        /// <param name="builder">Full node builder to which inject persistence provider manager.</param>
        /// <param name="persistenceProviderManager">The persistence provider manager to use to.</param>
        /// <returns>Interface to allow fluent code.</returns>
        public static IFullNodeBuilder UsePersistenceProviderMananger(this IFullNodeBuilder builder, IPersistenceProviderManager persistenceProviderManager)
        {
            var nodeBuilder = builder as FullNodeBuilder;
            nodeBuilder.PersistenceProviderManager = persistenceProviderManager;

            return builder;
        }
    }
}
