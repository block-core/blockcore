using System.Collections.Generic;
using System.Linq;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration;
using Blockcore.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Utilities.Store
{
    /// <inheritdoc/>
    public class PersistenceProviderManager : IPersistenceProviderManager
    {
        protected readonly NodeSettings nodeSettings;
        protected readonly Dictionary<string, IPersistenceProvider> persistenceProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceProviderManager"/> class.
        /// This class handles the initialization of persistence implementors for specific features on behalf of other features.
        /// For example if BlockStore feature requires a persistence, it can call
        /// </summary>
        /// <param name="nodeSettings">The settings from which obtain the default db type.</param>
        /// <param name="persistenceProviders">The persistence providers.</param>
        public PersistenceProviderManager(NodeSettings nodeSettings, params IPersistenceProvider[] persistenceProviders)
        {
            this.nodeSettings = nodeSettings;
            this.persistenceProviders = persistenceProviders.ToDictionary(provider => provider.Name.ToLowerInvariant());
        }

        /// <inheritdoc/>
        public IPersistenceProviderManager AddProvider(IPersistenceProvider provider)
        {
            this.persistenceProviders[provider.Name.ToLowerInvariant()] = provider;
            return this;
        }

        /// <inheritdoc/>
        public void RequirePersistence<TFeature>(IServiceCollection services, string persistenceProviderImplementation = null) where TFeature : IFullNodeFeature
        {
            if (persistenceProviderImplementation == null)
            {
                persistenceProviderImplementation = this.nodeSettings.DbType.ToString();
            }

            if (this.persistenceProviders.TryGetValue(persistenceProviderImplementation.ToLowerInvariant(), out IPersistenceProvider persistenceProvider))
            {
                persistenceProvider.AddRequiredServices<TFeature>(services);
            }
            else
            {
                throw new NodeBuilderException($"Required persistence provider {persistenceProviderImplementation} doesn't implement persistence for {typeof(TFeature).Name}.");
            }
        }

    }
}
