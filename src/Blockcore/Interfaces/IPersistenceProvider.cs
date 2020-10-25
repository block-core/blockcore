using Blockcore.Builder.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Interfaces
{
    /// <summary>
    /// Allow features to request a persistence implementation.
    /// </summary>
    public interface IPersistenceProvider
    {
        /// <summary>
        /// Gets the name of the persistence implementation (usually is the name of the db engine used, e.g. "litedb" or "rocksdb".
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Add required services for the specified feature type <typeparamref name="TFeature"/>.
        /// </summary>
        /// <typeparam name="TFeature">Feature type requesting initialization.</typeparam>
        void AddRequiredServices<TFeature>(IServiceCollection services) where TFeature : IFullNodeFeature;
    }
}
