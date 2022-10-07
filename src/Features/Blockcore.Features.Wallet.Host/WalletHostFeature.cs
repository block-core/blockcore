using System.Threading.Tasks;
using Blockcore.Broadcasters;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Features.Wallet.Broadcasters;
using Blockcore.Features.Wallet.UI;
using Blockcore.Interfaces.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Features.Wallet
{
    public class WalletHostFeature : FullNodeFeature
    {
        public WalletHostFeature()
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

    public static class FullNodeBuilderWalletHostExtension
    {
        public static IFullNodeBuilder UseWalletHost(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<WalletHostFeature>()
                .DependOn<WalletFeature>()
                .FeatureServices(services =>
                    {                   
                        services.AddSingleton<INavigationItem, WalletNavigationItem>();
                        services.AddSingleton<IClientEventBroadcaster, WalletInfoBroadcaster>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}