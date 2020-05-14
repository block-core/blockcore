using System;
using Blockcore.Broadcasters;

namespace Blockcore.EventBus
{
    /// <summary>
    /// Basic abstract implementation of <see cref="IEvent"/>.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase.IEvent" />
    public abstract class EventBase
    {
        public Guid CorrelationId { get; }

        public EventBase()
        {
            // Assigns an unique id to the event.
            this.CorrelationId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return $"{this.CorrelationId.ToString()} - {this.GetType().Name}";
        }

        public string EventName { get { return this.GetType().Name.ToLowerInvariant(); } }

        public string EventType { get { return this.GetType().ToString(); } }
    }
}