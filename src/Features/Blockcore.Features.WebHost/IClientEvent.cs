using System;
using Blockcore.EventBus;

namespace Blockcore.Features.WebHost
{
    public interface IClientEvent
    {
        Type NodeEventType { get; }

        void BuildFrom(EventBase @event);
    }
}