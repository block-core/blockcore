using System;
using System.Collections.Generic;
using Blockcore.AsyncWork;
using Blockcore.EventBus;
using Blockcore.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Blockcore.Broadcasters
{
    /// <summary>
    /// Base class for all Web Socket Broadcasters
    /// </summary>
    public abstract class ClientBroadcasterBase : IClientEventBroadcaster
    {
        private readonly IEventsSubscriptionService eventsHub;
        private readonly INodeLifetime nodeLifetime;
        private readonly IAsyncProvider asyncProvider;
        private IAsyncLoop asyncLoop;
        protected readonly ILogger log;

        protected ClientBroadcasterBase(
            IEventsSubscriptionService eventsHub,
            ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime,
            IAsyncProvider asyncProvider)
        {
            this.eventsHub = eventsHub;
            this.nodeLifetime = nodeLifetime;
            this.asyncProvider = asyncProvider;
            this.log = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public void Init(ClientEventBroadcasterSettings broadcasterSettings)
        {
            this.log.LogDebug($"Initialising Web Socket Broadcaster {this.GetType().Name}");

            this.asyncLoop = this.asyncProvider.CreateAndRunAsyncLoop(
                $"Broadcast {this.GetType().Name}",
                async token =>
                {
                    foreach (EventBase clientEvent in this.GetMessages())
                    {
                        this.eventsHub.OnEvent(clientEvent);
                    }
                },
                this.nodeLifetime.ApplicationStopping,
                repeatEvery: TimeSpan.FromSeconds(Math.Max(broadcasterSettings.BroadcastFrequencySeconds, 5)),
                startAfter: TimeSpans.FiveSeconds);
        }

        protected abstract IEnumerable<EventBase> GetMessages();
    }
}