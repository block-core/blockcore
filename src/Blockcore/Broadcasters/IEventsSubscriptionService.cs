using System.Threading.Tasks;
using Blockcore.Broadcasters;
using Blockcore.EventBus;
using Microsoft.AspNetCore.SignalR;

namespace Blockcore.Broadcasters
{
    public interface IEventsSubscriptionService
    {
        void Init();

        bool HasConsumers { get; }

        void Subscribe(string id, string name);

        void Unsubscribe(string id, string name);

        void UnsubscribeAll(string id);

        void OnEvent(EventBase @event);

        void SetHub<T>(IHubContext<T> hubContext) where T : Hub;
    }
}