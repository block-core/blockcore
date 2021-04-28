using System.Collections.Generic;
using Blockcore.Builder.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Persistence
{
    /// <summary>
    /// Used by features to require a persistence implementation.
    /// </summary>
    public interface IPersistenceProviderManager
    {
        /// <summary>
        /// Requires the persistence implementation for the <typeparamref name="TFeature"/> feature type.
        /// </summary>
        /// <typeparam name="TFeature">The type of the feature.</typeparam>
        /// <param name="services">The services collection IPersistenceProvider will use to register persistence component needed by the specified feature.</param>
        /// <param name="persistenceProviderImplementation">The explicit persistence provider implementation. If null, the one specified by dbtype argument will be used.</param>
        void RequirePersistence<TFeature>(IServiceCollection services, string persistenceProviderImplementation = null) where TFeature : IFullNodeFeature;

        /// <summary>
        /// Initializes the persistence provider manager, loading known persistence implementations based on the implementer strategy.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets the available providers.
        /// </summary>
        IEnumerable<string> GetAvailableProviders();

        /// <summary>
        /// Gets the default provider.
        /// </summary>
        string GetDefaultProvider();
    }
}