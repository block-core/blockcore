using System;
using System.Text;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Features.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Dashboard
{
    /// <summary>
    /// Provides an Api to the full node
    /// </summary>
    public sealed class DashbordFeature : FullNodeFeature
    {
        /// <summary>How long we are willing to wait for the API to stop.</summary>
        private const int DashboardStopTimeoutSeconds = 10;

        private readonly IFullNodeBuilder fullNodeBuilder;

        private readonly FullNode fullNode;

        private readonly DashboardSettings dashboardSettings;

        private readonly ILogger logger;

        private IHost host;

        private readonly ICertificateStore certificateStore;

        public DashbordFeature(
            IFullNodeBuilder fullNodeBuilder,
            FullNode fullNode,
            ApiFeatureOptions apiFeatureOptions,
            DashboardSettings apiSettings,
            ILoggerFactory loggerFactory,
            ICertificateStore certificateStore)
        {
            this.fullNodeBuilder = fullNodeBuilder;
            this.fullNode = fullNode;
            this.dashboardSettings = apiSettings;
            this.certificateStore = certificateStore;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public override Task InitializeAsync()
        {
            this.logger.LogInformation("Dashboard UI starting on URL '{0}'.", this.dashboardSettings.DashboardUri);
            var hostBuilder = Program.CreateHostBuilder(new string[0], this.fullNode, this.fullNodeBuilder.Services, this.dashboardSettings);

            this.host = hostBuilder.Build();
            this.host.RunAsync();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            DashboardSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            DashboardSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            // Make sure we are releasing the listening ip address / port.
            if (this.host != null)
            {
                this.logger.LogInformation("Dashboard UI stopping on URL '{0}'.", this.dashboardSettings.DashboardUri);
                this.host.StopAsync(TimeSpan.FromSeconds(DashboardStopTimeoutSeconds)).Wait();
                this.host = null;
            }
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class DashboardFeatureExtension
    {
        public static IFullNodeBuilder UseUI(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<DashbordFeature>()
                .FeatureServices(services =>
                    {
                        services.AddSingleton(fullNodeBuilder);
                        services.AddSingleton<DashboardSettings>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}