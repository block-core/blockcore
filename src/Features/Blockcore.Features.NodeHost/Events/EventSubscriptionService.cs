using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Blockcore.Broadcasters;
using Blockcore.EventBus;
using Blockcore.Features.NodeHost.Hubs;
using Blockcore.Signals;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.NodeHost.Events
{
    /// <summary>
    /// This class subscribes to Blockcore.EventBus.EventBus messages and proxy's them
    /// to Web Socket messages.
    /// </summary>
    public class EventSubscriptionService : IEventsSubscriptionService, IDisposable
    {
        internal class SubscriptionList
        {
            internal List<string> Events { get; set; } = new List<string>();
        }

        private readonly ISignals signals;
        private readonly ILogger<EventSubscriptionService> log;

        private readonly ConcurrentDictionary<string, SubscriptionToken> subscriptions = new ConcurrentDictionary<string, SubscriptionToken>();
        private readonly ConcurrentDictionary<string, SubscriptionList> consumers = new ConcurrentDictionary<string, SubscriptionList>();
        private readonly ConcurrentDictionary<string, Type> events = new ConcurrentDictionary<string, Type>();

        private IHubContext<EventsHub> hubContext;

        public EventSubscriptionService(
            ILoggerFactory logger,
            ISignals signals)
        {
            this.log = logger.CreateLogger<EventSubscriptionService>(); ;
            this.signals = signals;
        }

        public void Init()
        {
            Type baseType = typeof(EventBase);

            // Get all events that is loaded on the node.
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetLoadableTypes().Where(t => baseType.IsAssignableFrom(t)));

            // Make sure all the events are available to be subscribed to.
            foreach (Type type in types)
            {
                this.events.AddOrReplace(type.Name.ToLowerInvariant(), type);
            }
        }

        public bool HasConsumers => this.consumers.Count > 0;

        public void SetHub<T>(IHubContext<T> hubContext) where T : Hub
        {
            Guard.Assert(hubContext is IHubContext<EventsHub>);
            this.hubContext = (IHubContext<EventsHub>)hubContext;
        }

        private void SubscribeToEvent(string name)
        {
            Type eventToSubscribe = this.events.GetValueOrDefault(name);

            if (eventToSubscribe == null)
            {
                throw new ApplicationException($"The event does not exists \"{name}\".");
            }

            MethodInfo subscribeMethod = this.signals.GetType().GetMethod("Subscribe");
            MethodInfo onEventCallbackMethod = typeof(EventSubscriptionService).GetMethod("OnEvent");

            this.log.LogDebug("Create subscription for {0}", eventToSubscribe);

            MethodInfo subscribeMethodInfo = subscribeMethod.MakeGenericMethod(eventToSubscribe);
            Type callbackType = typeof(Action<>).MakeGenericType(eventToSubscribe);
            Delegate onEventDelegate = Delegate.CreateDelegate(callbackType, this, onEventCallbackMethod);

            var token = (SubscriptionToken)subscribeMethodInfo.Invoke(this.signals, new object[] { onEventDelegate });
            var added = this.subscriptions.TryAdd(name, token);

            // If we did not add this successful, unsubscribe again to avoid leaking subscriptions to events.
            if (!added)
            {
                // Dispose the token, it will unsubscribe.
                token.Dispose();
            }
        }

        private void UnsubscribeToEvent(string name)
        {
            if (this.subscriptions.ContainsKey(name))
            {
                this.subscriptions[name].Dispose();
            }
        }

        public void Subscribe(string id, string name)
        {
            name = name.ToLowerInvariant();

            SubscriptionList consumer = this.consumers.GetOrAdd(id, x => new SubscriptionList()); ;

            if (consumer.Events.Contains(name))
            {
                return;
            }

            this.log.LogInformation("The {id} is subscribing to \"{name}\" event.", id, name);

            consumer.Events.Add(name);

            // If we are currently not subscribing to this event, add a subscription to it.
            if (!this.subscriptions.ContainsKey(name))
            {
                SubscribeToEvent(name);
            }
        }

        public void Unsubscribe(string id, string name)
        {
            name = name.ToLowerInvariant();

            if (!this.consumers.ContainsKey(id))
            {
                return;
            }

            this.log.LogInformation("The {id} is unsubscribing to \"{name}\" event.", id, name);

            SubscriptionList consumer = this.consumers[id];

            if (consumer.Events.Contains(name))
            {
                consumer.Events.Remove(name);
            }

            if (consumer.Events.Count == 0)
            {
                this.consumers.TryRemove(name, out _);
            }

            // If there are no longer any subscribers to this event, remove the subscription to it.
            if (!this.consumers.Any(c => c.Value.Events.Contains(name)))
            {
                UnsubscribeToEvent(name);
            }
        }

        /// <summary>
        /// Call to unsubscribe to all events registered on this consumer Id.
        /// </summary>
        /// <param name="id"></param>
        public void UnsubscribeAll(string id)
        {
            if (!this.consumers.ContainsKey(id))
            {
                return;
            }

            this.log.LogInformation("The {id} is unsubscribing to all events.", id);

            SubscriptionList consumer = this.consumers[id];

            if (consumer == null)
            {
                return;
            }

            // Get all IDs to remove in separate list so code doesn't crash when Unsubscribe changes the collection.
            var events = consumer.Events.ToList();
            events.ForEach(e => Unsubscribe(id, e));

            this.consumers.TryRemove(id, out _);
        }

        public void OnEvent(EventBase @event)
        {
            if (this.hubContext != null && @event != null)
            {
                List<string> consumersToInform = this.consumers.Where(c => c.Value.Events.Contains(@event.EventName))
                         .Select(c => c.Key).ToList();


                if (consumersToInform.Count > 0)
                {
                    try
                    {
                        //this.hubContext.Clients.Clients(consumersToInform).SendAsync("ReceiveEvent", @event).ConfigureAwait(false).GetAwaiter().GetResult();
                        this.hubContext.Clients.Clients(consumersToInform).SendAsync("ReceiveEvent", @event).ConfigureAwait(true); // .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        this.log.LogError(ex, "Failed to send event to consumer.");
                    }
                }
            }
        }

        public void Dispose()
        {
            // Dispose all the subscriptions.
            List<SubscriptionToken> subs = this.subscriptions.Select(s => s.Value).ToList();
            subs.ForEach(s => s?.Dispose());
        }
    }
}