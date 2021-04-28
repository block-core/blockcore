using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Broadcasters;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.EventBus;
using Blockcore.Features.NodeHost.Events;
using Blockcore.Features.NodeHost.Hubs;
using Blockcore.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.NodeHost
{
    /// <summary>
    /// Provides an Api to the full node
    /// </summary>
    public sealed class NodeHostFeature : FullNodeFeature
    {
        /// <summary>How long we are willing to wait for the NodeHost to stop.</summary>
        private const int NodeHostStopTimeoutSeconds = 10;

        private readonly IFullNodeBuilder fullNodeBuilder;

        private readonly FullNode fullNode;

        private readonly NodeHostSettings settings;

        private readonly IEnumerable<IClientEventBroadcaster> eventBroadcasters;

        private readonly IEventsSubscriptionService eventsSubscriptionService;

        private readonly ILogger logger;

        private IWebHost webHost;

        private readonly ICertificateStore certificateStore;

        public NodeHostFeature(
            IFullNodeBuilder fullNodeBuilder,
            FullNode fullNode,
            NodeHostSettings apiSettings,
            ILoggerFactory loggerFactory,
            ICertificateStore certificateStore,
            IEnumerable<IClientEventBroadcaster> eventBroadcasters,
            IEventsSubscriptionService eventsSubscriptionService)
        {
            this.fullNodeBuilder = fullNodeBuilder;
            this.fullNode = fullNode;
            this.settings = apiSettings;
            this.certificateStore = certificateStore;
            this.eventBroadcasters = eventBroadcasters;
            this.eventsSubscriptionService = eventsSubscriptionService;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);

            this.InitializeBeforeBase = true;
        }

        public override Task InitializeAsync()
        {
            string log = $"NodeHost listening on: {Environment.NewLine}{this.settings.ApiUri}{Environment.NewLine}  - UI: {this.settings.EnableUI}{Environment.NewLine}  - API: {this.settings.EnableAPI}{Environment.NewLine}  - WS: {this.settings.EnableWS}";
            this.logger.LogInformation(log);

            this.eventsSubscriptionService.Init();

            foreach (IClientEventBroadcaster clientEventBroadcaster in this.eventBroadcasters)
            {
                // Intialise with specified settings
                //clientEventBroadcaster.Init(eventBroadcasterSettings[clientEventBroadcaster.GetType()]);
                clientEventBroadcaster.Init(new ClientEventBroadcasterSettings { BroadcastFrequencySeconds = 5 });
            }

            this.webHost = Program.Initialize(this.fullNodeBuilder.Services, this.fullNode, this.settings, this.certificateStore, new WebHostBuilder());

            return Task.CompletedTask;
        }

        /// <summary>
        /// Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            NodeHostSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            NodeHostSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            // Make sure we are releasing the listening ip address / port.
            if (this.webHost != null)
            {
                this.logger.LogInformation("API stopping on URL '{0}'.", this.settings.ApiUri);
                this.webHost.StopAsync(TimeSpan.FromSeconds(NodeHostStopTimeoutSeconds)).Wait();
                this.webHost = null;
            }
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class NodeHostFeatureExtension
    {
        public static IFullNodeBuilder UseNodeHost(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<NodeHostFeature>()
                .FeatureServices(services =>
                {
                    services.AddSingleton(fullNodeBuilder);
                    services.AddSingleton<NodeHostSettings>();
                    services.AddSingleton<IEventsSubscriptionService, EventSubscriptionService>();
                    services.AddSingleton<ICertificateStore, CertificateStore>();
                });
            });

            return fullNodeBuilder;
        }
    }
}