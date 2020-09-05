using System;
using System.Threading.Tasks;
using Blockcore.Broadcasters;
using Blockcore.EventBus;
using Blockcore.Features.NodeHost.Events;
using Blockcore.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.NodeHost.Hubs
{
    public class EventsHub : Hub
    {
        private readonly ILogger<EventsHub> logger;

        private readonly IEventsSubscriptionService eventsSubscriptionService;

        public EventsHub(ILoggerFactory loggerFactory, IEventsSubscriptionService eventsSubscriptionService)
        {
            this.logger = loggerFactory.CreateLogger<EventsHub>();

            this.eventsSubscriptionService = eventsSubscriptionService;
        }

        public override Task OnConnectedAsync()
        {
            this.logger.LogDebug("New client with id {id} connected", this.Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            this.logger.LogDebug("Client with id {id} disconnected", this.Context.ConnectionId);

            this.eventsSubscriptionService.UnsubscribeAll(this.Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }

        public Task Subscribe(params string[] events)
        {
            foreach (var @event in events)
            { 
                this.eventsSubscriptionService.Subscribe(this.Context.ConnectionId, @event);
            }

            return Task.CompletedTask;
        }

        public Task Unsubscribe(params string[] events)
        {
            foreach (var @event in events)
            {
                this.eventsSubscriptionService.Unsubscribe(this.Context.ConnectionId, @event);
            }

            return Task.CompletedTask;
        }
    }
}