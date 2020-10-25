using Blockcore.Builder.Feature;
using Blockcore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Utilities.Store
{
    /// <summary>
    /// Used by features to require a persistence implementation.
    /// </summary>
    public interface IPersistenceProviderManager
    {
        /// <summary>
        /// Adds a persistence provider to known providers.
        /// </summary>
        /// <param name="provider">The persistence provider to add.</param>
        /// <returns></returns>
        IPersistenceProviderManager AddProvider(IPersistenceProvider provider);

        /// <summary>
        /// Requires the persistence implementation for the <typeparamref name="TFeature"/> feature type.
        /// </summary>
        /// <typeparam name="TFeature">The type of the feature.</typeparam>
        /// <param name="services">The services collection IPersistenceProvider will use to register persistence component needed by the specified feature.</param>
        /// <param name="persistenceProviderImplementation">The explicit persistence provider implementation. If null, the one specified by dbtype argument will be used.</param>
        void RequirePersistence<TFeature>(IServiceCollection services, string persistenceProviderImplementation = null) where TFeature : IFullNodeFeature;
    }
}