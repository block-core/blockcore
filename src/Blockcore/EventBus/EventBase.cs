using System;

namespace Blockcore.EventBus
{
    /// <summary>
    /// Basic abstract implementation of <see cref="IEvent"/>.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.IEvent" />
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
    }
}