using System;
using Blockcore.EventBus;

namespace Blockcore.Features.SignalR
{
    public interface IClientEvent
    {
        Type NodeEventType { get; }

        void BuildFrom(EventBase @event);
    }
}