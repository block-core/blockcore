using System;
using System.Collections.Generic;
using Blockcore.AsyncWork;
using Blockcore.Features.WebHost.Hubs;
using Blockcore.Features.WebHost.Options;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.WebHost.Broadcasters
{
    /// <summary>
    /// Base class for all Web Socket Broadcasters
    /// </summary>
    public abstract class ClientBroadcasterBase : IClientEventBroadcaster
    {
        private readonly EventsHub eventsHub;
        private readonly INodeLifetime nodeLifetime;
        private readonly IAsyncProvider asyncProvider;
        protected readonly ILogger logger;
        private IAsyncLoop asyncLoop;

        protected ClientBroadcasterBase(
            EventsHub eventsHub,
            ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime,
            IAsyncProvider asyncProvider)
        {
            this.eventsHub = eventsHub;
            this.nodeLifetime = nodeLifetime;
            this.asyncProvider = asyncProvider;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public void Init(ClientEventBroadcasterSettings broadcasterSettings)
        {
            this.logger.LogDebug($"Initialising Web Socket Broadcaster {this.GetType().Name}");
            this.asyncLoop = this.asyncProvider.CreateAndRunAsyncLoop(
                $"Broadcast {this.GetType().Name}",
                async token =>
                {
                    foreach (IClientEvent clientEvent in this.GetMessages())
                    {
                        await this.eventsHub.SendToClientsAsync(clientEvent).ConfigureAwait(false);
                    }
                },
                this.nodeLifetime.ApplicationStopping,
                repeatEvery: TimeSpan.FromSeconds(Math.Max(broadcasterSettings.BroadcastFrequencySeconds, 5)));
        }

        protected abstract IEnumerable<IClientEvent> GetMessages();
    }
}