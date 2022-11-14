using System;

namespace Blockcore.EventBus
{
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
            return $"{this.CorrelationId.ToString()} - {GetType().Name}";
        }

        public string EventName { get { return GetType().Name.ToLowerInvariant(); } }
    }
}