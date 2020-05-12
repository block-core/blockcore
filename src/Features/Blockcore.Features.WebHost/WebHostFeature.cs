using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Features.WebHost.Broadcasters;
using Blockcore.Features.WebHost.Events;
using Blockcore.Features.WebHost.Hubs;
using Blockcore.Features.WebHost.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.WebHost
{
    /// <summary>
    /// Provides an Api to the full node
    /// </summary>
    public sealed class WebHostFeature : FullNodeFeature
    {
        internal static Dictionary<Type, ClientEventBroadcasterSettings> eventBroadcasterSettings;

        /// <summary>How long we are willing to wait for the WebHost to stop.</summary>
        private const int WebHostStopTimeoutSeconds = 10;

        private readonly IFullNodeBuilder fullNodeBuilder;

        private readonly FullNode fullNode;

        private readonly WebHostSettings settings;

        private readonly WebHostFeatureOptions featureOptions;

        private readonly IEnumerable<IClientEventBroadcaster> eventBroadcasters;

        private readonly IEventsSubscriptionService eventsSubscriptionService;

        private readonly ILogger logger;

        private IWebHost webHost;

        private readonly ICertificateStore certificateStore;

        public WebHostFeature(
            IFullNodeBuilder fullNodeBuilder,
            FullNode fullNode,
            WebHostFeatureOptions apiFeatureOptions,
            WebHostSettings apiSettings,
            ILoggerFactory loggerFactory,
            ICertificateStore certificateStore,
            IEnumerable<IClientEventBroadcaster> eventBroadcasters,
            IEventsSubscriptionService eventsSubscriptionService)
        {
            this.fullNodeBuilder = fullNodeBuilder;
            this.fullNode = fullNode;
            this.featureOptions = apiFeatureOptions;
            this.settings = apiSettings;
            this.certificateStore = certificateStore;
            this.eventBroadcasters = eventBroadcasters;
            this.eventsSubscriptionService = eventsSubscriptionService;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);

            this.InitializeBeforeBase = true;
        }

        public override Task InitializeAsync()
        {
            string log = $"WebHost listening on: {Environment.NewLine}{this.settings.ApiUri}{Environment.NewLine}  - UI: {this.settings.EnableUI}{Environment.NewLine}  - API: {this.settings.EnableAPI}{Environment.NewLine}  - WS: {this.settings.EnableWS}";
            this.logger.LogInformation(log);

            this.eventsSubscriptionService.Init();

            foreach (IClientEventBroadcaster clientEventBroadcaster in this.eventBroadcasters)
            {
                // Intialise with specified settings
                clientEventBroadcaster.Init(eventBroadcasterSettings[clientEventBroadcaster.GetType()]);
            }

            this.webHost = Program.Initialize(this.fullNodeBuilder.Services, this.fullNode, this.settings, this.certificateStore, new WebHostBuilder());

            if (this.settings.KeepaliveTimer == null)
            {
                this.logger.LogTrace("(-)[KEEPALIVE_DISABLED]");
                return Task.CompletedTask;
            }

            // Start the keepalive timer, if set.
            // If the timer expires, the node will shut down.
            this.settings.KeepaliveTimer.Elapsed += (sender, args) =>
            {
                this.logger.LogInformation($"The application will shut down because the keepalive timer has elapsed.");

                this.settings.KeepaliveTimer.Stop();
                this.settings.KeepaliveTimer.Enabled = false;
                this.fullNode.NodeLifetime.StopApplication();
            };

            this.settings.KeepaliveTimer.Start();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            WebHostSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            WebHostSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            // Make sure the timer is stopped and disposed.
            if (this.settings.KeepaliveTimer != null)
            {
                this.settings.KeepaliveTimer.Stop();
                this.settings.KeepaliveTimer.Enabled = false;
                this.settings.KeepaliveTimer.Dispose();
            }

            // Make sure we are releasing the listening ip address / port.
            if (this.webHost != null)
            {
                this.logger.LogInformation("API stopping on URL '{0}'.", this.settings.ApiUri);
                this.webHost.StopAsync(TimeSpan.FromSeconds(WebHostStopTimeoutSeconds)).Wait();
                this.webHost = null;
            }
        }
    }

    public sealed class WebHostFeatureOptions
    {
        public IClientEvent[] EventsToHandle { get; set; }

        public (Type Broadcaster, ClientEventBroadcasterSettings clientEventBroadcasterSettings)[] ClientEventBroadcasters { get; set; }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class ApiFeatureExtension
    {
        [Obsolete("Use the new UseWebHost()")]
        public static IFullNodeBuilder UseApi(this IFullNodeBuilder fullNodeBuilder, Action<WebHostFeatureOptions> optionsAction = null)
        {
            return UseWebHost(fullNodeBuilder, optionsAction);
        }

        public static IFullNodeBuilder UseWebHost(this IFullNodeBuilder fullNodeBuilder, Action<WebHostFeatureOptions> optionsAction = null)
        {
            // TODO: move the options in to the feature builder
            var options = new WebHostFeatureOptions();
            optionsAction?.Invoke(options);

            // If there is no events to handle defined in the options configured by the node, add a few default.
            if (options.EventsToHandle == null)
            {
                options.EventsToHandle = new[]
                {
                    (IClientEvent) new BlockConnectedClientEvent(),
                    new TransactionReceivedClientEvent()
                };
            }

            // If there is now client event broadcasters, add the default ones.
            if (options.ClientEventBroadcasters == null)
            {
                options.ClientEventBroadcasters = new[]
                {
                    (Broadcaster: typeof(StakingBroadcaster), ClientEventBroadcasterSettings: new ClientEventBroadcasterSettings { BroadcastFrequencySeconds = 5 }),
                    (Broadcaster: typeof(WalletInfoBroadcaster), ClientEventBroadcasterSettings: new ClientEventBroadcasterSettings { BroadcastFrequencySeconds = 5 })
                };
            }

            WebHostFeature.eventBroadcasterSettings = options.ClientEventBroadcasters.ToDictionary(pair => pair.Broadcaster, pair => pair.clientEventBroadcasterSettings);

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<WebHostFeature>()
                .FeatureServices(services =>
                {
                    services.AddSingleton(fullNodeBuilder);
                    services.AddSingleton(options);
                    services.AddSingleton<WebHostSettings>();
                    services.AddSingleton<IEventsSubscriptionService, EventSubscriptionService>();
                    services.AddSingleton<EventsHub>();
                    services.AddSingleton<ICertificateStore, CertificateStore>();

                    if (null != options.ClientEventBroadcasters)
                    {
                        foreach ((Type Broadcaster, ClientEventBroadcasterSettings clientEventBroadcasterSettings) in options.ClientEventBroadcasters)
                        {
                            if (typeof(IClientEventBroadcaster).IsAssignableFrom(Broadcaster))
                            {
                                services.AddSingleton(typeof(IClientEventBroadcaster), Broadcaster);
                            }
                            else
                            {
                                Console.WriteLine($"Warning {Broadcaster.Name} is not of type {typeof(IClientEventBroadcaster).Name}");
                            }
                        }
                    }
                });
            });

            return fullNodeBuilder;
        }
    }
}
