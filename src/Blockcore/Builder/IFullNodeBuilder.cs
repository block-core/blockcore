using System;
using Blockcore.Builder.Feature;
using Blockcore.Configuration;
using Blockcore.Networks;
using Blockcore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Builder
{
    /// <summary>
    /// Full node builder allows constructing a full node using specific components.
    /// </summary>
    public interface IFullNodeBuilder
    {
        /// <summary>User defined node settings.</summary>
        NodeSettings NodeSettings { get; }

        /// <summary>Allows to require implementation of the persistence layer they need.</summary>
        IPersistenceProviderManager PersistenceProviderManager { get; }

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        Network Network { get; }

        /// <summary>Collection of DI services.</summary>
        IServiceCollection Services { get; }

        /// <summary>Collection of features</summary>
        IFeatureCollection Features { get; }

        /// <summary>
        /// Constructs the full node with the required features, services, and settings.
        /// </summary>
        /// <returns>Initialized full node.</returns>
        IFullNode Build();

        /// <summary>
        /// Adds features to the builder.
        /// </summary>
        /// <param name="configureFeatures">A method that adds features to the collection.</param>
        /// <returns>Interface to allow fluent code.</returns>
        IFullNodeBuilder ConfigureFeature(Action<IFeatureCollection> configureFeatures);

        /// <summary>
        /// Adds services to the builder.
        /// </summary>
        /// <param name="configureServices">A method that adds services to the builder.</param>
        /// <returns>Interface to allow fluent code.</returns>
        IFullNodeBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        /// <summary>
        /// Add configurations for the service provider.
        /// </summary>
        /// <param name="configure">A method that configures the service provider.</param>
        /// <returns>Interface to allow fluent code.</returns>
        IFullNodeBuilder ConfigureServiceProvider(Action<IServiceProvider> configure);
    }
}
