using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Blockcore.Broadcasters;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration.Logging;
using Blockcore.Features.Miner.Broadcasters;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.Miner.UI;
using Blockcore.Interfaces.UI;
using Blockcore.Mining;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Blockcore.Features.Miner.Tests")]

namespace Blockcore.Features.Miner
{
    public class MiningHostFeature : FullNodeFeature
    {

        public MiningHostFeature()
        {
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }
    }

    public static class FullNodeBuilderMiningExtension
    {
        public static IFullNodeBuilder AddMiningHost(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<MiningHostFeature>("mining");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<MiningHostFeature>()
                    .DependOn<MiningFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IPowMining, PowMining>();
                        services.AddSingleton<IBlockProvider, BlockProvider>();
                        services.AddSingleton<BlockDefinition, PowBlockDefinition>();
                        services.AddSingleton<MinerSettings>();
                    });
            });

            return fullNodeBuilder;
        }

        public static IFullNodeBuilder AddPowPosMiningHost(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<MiningHostFeature>("mining");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<MiningHostFeature>()
                    .DependOn<MiningFeature>()
                    .FeatureServices(services =>
                    {
 
                        services.AddSingleton<INavigationItem, StakeNavigationItem>();
                        services.AddSingleton<INavigationItem, MineNavigationItem>();
                        services.AddSingleton<IClientEventBroadcaster, StakingBroadcaster>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}