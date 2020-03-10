using Microsoft.Extensions.Logging;
using Blockcore.EventBus;

namespace Blockcore.Signals
{
    public interface ISignals : IEventBus
    {
    }

    public class Signals : InMemoryEventBus, ISignals
    {
        public Signals(ILoggerFactory loggerFactory, ISubscriptionErrorHandler subscriptionErrorHandler) : base(loggerFactory, subscriptionErrorHandler) { }
    }
}
