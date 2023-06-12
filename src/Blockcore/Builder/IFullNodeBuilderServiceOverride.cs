using System;
using Blockcore.Builder.Feature;
using Blockcore.Configuration;
using Blockcore.Networks;
using Blockcore.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Builder
{
    /// <summary>
    /// Allow specific network implementation to override services.
    /// </summary>
    public interface IFullNodeBuilderServiceOverride
    {
        /// <summary>
        /// Intercept the builder to override services.
        /// </summary>
        void OverrideServices(IFullNodeBuilder builder);
    }
}
