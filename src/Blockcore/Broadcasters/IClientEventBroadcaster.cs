using Microsoft.AspNetCore.SignalR;

namespace Blockcore.Broadcasters
{
    public interface IClientEventBroadcaster
    {
        void Init(ClientEventBroadcasterSettings broadcasterSettings);
    }
}