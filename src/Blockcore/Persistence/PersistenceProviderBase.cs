using System;
using Blockcore.Builder.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Persistence
{
    /// <inheritdoc/>
    public abstract class PersistenceProviderBase<TFeature> : IPersistenceProvider where TFeature : IFullNodeFeature
    {
        /// <inheritdoc/>
        public abstract string Tag { get; }

        /// <inheritdoc/>
        public Type FeatureType => typeof(TFeature);

        /// <inheritdoc/>
        public abstract void AddRequiredServices(IServiceCollection services);
    }
}
