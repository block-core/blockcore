using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Persistence
{
    /// <inheritdoc/>
    public class PersistenceProviderManager : IPersistenceProviderManager
    {
        protected readonly NodeSettings nodeSettings;
        protected readonly Dictionary<string, List<IPersistenceProvider>> persistenceProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceProviderManager"/> class.
        /// This class handles the initialization of persistence implementors for specific features on behalf of other features.
        /// For example if BlockStore feature requires a persistence, it can call
        /// </summary>
        /// <param name="nodeSettings">The settings from which obtain the default db type.</param>
        public PersistenceProviderManager(NodeSettings nodeSettings)
        {
            this.nodeSettings = nodeSettings;
            this.persistenceProviders = new Dictionary<string, List<IPersistenceProvider>>();
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetAvailableProviders()
        {
            return this.persistenceProviders.Keys;
        }

        /// <inheritdoc/>
        public string GetDefaultProvider()
        {
            if (this.persistenceProviders.Count == 0)
            {
                return null;
            }

            return this.persistenceProviders.ContainsKey("leveldb") ? "leveldb" : this.persistenceProviders.Keys.First();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Search for all assemblies that implement the interface IPersistenceProvider in all referenced libraries.
        /// Create one instance for each of them and register the instance in the <see cref="persistenceProviders"/> dictionary in order to be found when <see cref="RequirePersistence"/> will be called by any feature
        /// </remarks>
        public virtual void Initialize()
        {
            List<string> persistenceAssemblies = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.Persistence.*.dll").ToList();
            List<string> loadedPersistenceAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.Location).Where(path => path.Contains(".Persistence.")).ToList();

            List<string> unloadedPersistenceAssemblies = persistenceAssemblies.Except(loadedPersistenceAssemblies).ToList();

            foreach (string unloadedPersistenceAssembly in unloadedPersistenceAssemblies)
            {
                this.FindPersistenceAssembly(Assembly.LoadFrom(unloadedPersistenceAssembly));
            }
        }

        /// <summary>
        /// Finds the persistence implementation in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to search persistence implementation from.</param>
        private void FindPersistenceAssembly(Assembly assembly)
        {
            var type = typeof(IPersistenceProvider);

            // we could consider to add a parameter in configuration, to specify some rule to skip some of the discovered assemblies
            var persistenceProviderTypes = assembly
                .GetTypes()
                .Where(candidateType => !candidateType.IsAbstract && type.IsAssignableFrom(candidateType));

            foreach (Type providerType in persistenceProviderTypes)
            {
                IPersistenceProvider providerInstance = (IPersistenceProvider)Activator.CreateInstance(providerType);

                string implementationName = providerInstance.Tag.ToLowerInvariant();

                if (!this.persistenceProviders.TryGetValue(implementationName, out List<IPersistenceProvider> providersList))
                {
                    providersList = new List<IPersistenceProvider>();
                    this.persistenceProviders.Add(implementationName, providersList);
                }

                providersList.Add(providerInstance);
            }
        }

        /// <inheritdoc/>
        public void RequirePersistence<TFeature>(IServiceCollection services, string persistenceProviderImplementation = null) where TFeature : IFullNodeFeature
        {
            if (persistenceProviderImplementation == null)
            {
                persistenceProviderImplementation = this.nodeSettings.DbType ?? this.GetDefaultProvider();
            }

            IPersistenceProvider provider = null;

            if (this.persistenceProviders.TryGetValue(persistenceProviderImplementation.ToLowerInvariant(), out List<IPersistenceProvider> providersList))
            {
                provider = providersList.FirstOrDefault(provider => provider.FeatureType == typeof(TFeature));

                if (provider == null)
                {
                    throw new NodeBuilderException($"Required persistence provider {persistenceProviderImplementation} doesn't implement persistence for {typeof(TFeature).Name}.");
                }

                provider.AddRequiredServices(services);
            }
            else
            {
                throw new NodeBuilderException($"Required persistence provider implementation {persistenceProviderImplementation} Not found.");
            }
        }
    }
}
