using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime,
            IAsyncProvider asyncProvider,
            IEventsSubscriptionService subscriptionService = null
            )
        {
            this.eventsHub = subscriptionService;
            this.nodeLifetime = nodeLifetime;
            this.asyncProvider = asyncProvider;
            this.log = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public void Init(ClientEventBroadcasterSettings broadcasterSettings)
        {
            this.log.LogDebug($"Initialising Web Socket Broadcaster {this.GetType().Name}");

            this.asyncLoop = this.asyncProvider.CreateAndRunAsyncLoop(
                $"Broadcast {this.GetType().Name}",
                token =>
                {
                    if (this.eventsHub.HasConsumers)
                    {
                        foreach (EventBase clientEvent in this.GetMessages())
                        {
                            this.eventsHub.OnEvent(clientEvent);
                        }
                    }

                    return Task.CompletedTask;
                },
                this.nodeLifetime.ApplicationStopping,
                repeatEvery: TimeSpan.FromSeconds(Math.Max(broadcasterSettings.BroadcastFrequencySeconds, 5)),
                startAfter: TimeSpans.TenSeconds);
        }

        protected abstract IEnumerable<EventBase> GetMessages();
    }
}