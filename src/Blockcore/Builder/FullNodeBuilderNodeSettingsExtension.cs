using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Builder
{
    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderNodeSettingsExtension
    {
        /// <summary>
        /// Makes the full node builder use specific node settings.
        /// </summary>
        /// <param name="builder">Full node builder to change node settings for.</param>
        /// <param name="nodeSettings">Node settings to be used.</param>
        /// <param name="persistenceProviderManager">The persistence provider manager.</param>
        /// <returns>Interface to allow fluent code.</returns>
        public static IFullNodeBuilder UseNodeSettings(this IFullNodeBuilder builder, NodeSettings nodeSettings, IPersistenceProviderManager persistenceProviderManager)
        {
            var nodeBuilder = builder as FullNodeBuilder;
            nodeBuilder.NodeSettings = nodeSettings;
            nodeBuilder.Network = nodeSettings.Network;
            nodeBuilder.PersistenceProviderManager = persistenceProviderManager;

            builder.ConfigureServices(service =>
            {
                service.AddSingleton(nodeBuilder.NodeSettings);
                service.AddSingleton(nodeBuilder.Network);
            });

            return builder.UseBaseFeature();
        }

        /// <summary>
        /// Makes the full node builder use the default node settings.
        /// </summary>
        /// <param name="builder">Full node builder to change node settings for.</param>
        /// <returns>Interface to allow fluent code.</returns>
        public static IFullNodeBuilder UseDefaultNodeSettings(this IFullNodeBuilder builder, IPersistenceProviderManager persistenceProviderManager)
        {
            return builder.UseNodeSettings(NodeSettings.Default(builder.Network), persistenceProviderManager);
        }
    }
}
