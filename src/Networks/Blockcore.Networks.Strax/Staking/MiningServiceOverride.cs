using Blockcore.Builder;
using Blockcore.Features.Miner.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blockcore.Networks.Strax.Staking
{
    /// <summary>
    /// Full node builder allows constructing a full node using specific components.
    /// </summary>
    public class MiningServiceOverride : IFullNodeBuilderServiceOverride
    {
        public void OverrideServices(IFullNodeBuilder builder)
        {
            var replace = ServiceDescriptor.Singleton<IPosMinting, StraxMinting>();

            builder.Services.Replace(replace);
        }
    }
}
