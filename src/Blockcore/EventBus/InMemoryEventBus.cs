using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.EventBus
{
    public class InMemoryEventBus : IEventBus
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// The subscriber error handler
        /// </summary>
        private readonly ISubscriptionErrorHandler subscriptionErrorHandler;

        /// <summary>
        /// The subscriptions stored by EventType
        /// </summary>
        private readonly Dictionary<Type, List<ISubscription>> subscriptions;

        /// <summary>
        /// The subscriptions lock to prevent race condition during publishing
        /// </summary>
        private readonly object subscriptionsLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryEventBus"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="subscriptionErrorHandler">The subscription error handler. If null the default one will be used</param>
        public InMemoryEventBus(ILoggerFactory loggerFactory, ISubscriptionErrorHandler subscriptionErrorHandler)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.subscriptionErrorHandler = subscriptionErrorHandler ?? new DefaultSubscriptionErrorHandler(loggerFactory);
            this.subscriptions = new Dictionary<Type, List<ISubscription>>();
        }

        /// <inheritdoc />
        public SubscriptionToken Subscribe<TEvent>(Action<TEvent> action) where TEvent : EventBase
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (this.subscriptionsLock)
            {
                if (!this.subscriptions.ContainsKey(typeof(TEvent)))
                {
                    this.subscriptions.Add(typeof(TEvent), new List<ISubscription>());
                }

                var subscriptionToken = new SubscriptionToken(this, typeof(TEvent));
                this.subscriptions[typeof(TEvent)].Add(new Subscription<TEvent>(action, subscriptionToken));

                return subscriptionToken;
            }
        }

        /// <inheritdoc />
        public void Unsubscribe(SubscriptionToken subscriptionToken)
        {
            // Ignore null token
            if (subscriptionToken == null)
            {
                this.logger.LogDebug("Unsubscribe called with a null token, ignored.");
                return;
            }

            lock (this.subscriptionsLock)
            {
                if (this.subscriptions.ContainsKey(subscriptionToken.EventType))
                {
                    var allSubscriptions = this.subscriptions[subscriptionToken.EventType];

                    var subscriptionToRemove = allSubscriptions.FirstOrDefault(sub => sub.SubscriptionToken.Token == subscriptionToken.Token);
                    if (subscriptionToRemove != null)
                        this.subscriptions[subscriptionToken.EventType].Remove(subscriptionToRemove);
                }
            }
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent @eventItem) where TEvent : EventBase
        {
            if (@eventItem == null)
            {
                throw new ArgumentNullException(nameof(@eventItem));
            }

            List<ISubscription> allSubscriptions = new List<ISubscription>();

            lock (this.subscriptionsLock)
            {
                if (this.subscriptions.ContainsKey(typeof(TEvent)))
                {
                    allSubscriptions = this.subscriptions[typeof(TEvent)].ToList();
                }
            }

            for (var index = 0; index < allSubscriptions.Count; index++)
            {
                var subscription = allSubscriptions[index];

                try
                {
                    subscription.Publish(@eventItem);
                }
                catch (Exception ex)
                {
                    this.subscriptionErrorHandler?.Handle(@eventItem, ex, subscription);
                }
            }
        }
    }
}