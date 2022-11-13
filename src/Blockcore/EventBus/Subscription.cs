using System;

namespace Blockcore.EventBus
{
    internal class Subscription<TEventBase> : ISubscription where TEventBase : EventBase
    {
        /// <summary>
        /// Token returned to the subscriber
        /// </summary>
        public SubscriptionToken SubscriptionToken { get; }

        /// <summary>
        /// The action to invoke when a subscripted event type is published.
        /// </summary>
        private readonly Action<TEventBase> action;

        public Subscription(Action<TEventBase> action, SubscriptionToken token)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.SubscriptionToken = token ?? throw new ArgumentNullException(nameof(token));
        }

        public void Publish(EventBase eventBase)
        {
            if (!(eventBase is TEventBase))
                throw new ArgumentException("Event Item is not the correct type.");

            this.action.Invoke(eventBase as TEventBase);
        }
    }
}
