using System.Threading.Tasks;
using Blockcore.Broadcasters;
using Blockcore.EventBus;

namespace Blockcore.Broadcasters
{
    public interface IEventsSubscriptionService
    {
        void Init();

        void Subscribe(string id, string name);

        void Unsubscribe(string id, string name);

        void UnsubscribeAll(string id);

        void OnEvent(EventBase @event);
    }
}