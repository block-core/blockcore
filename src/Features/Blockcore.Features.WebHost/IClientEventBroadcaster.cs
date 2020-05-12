using Blockcore.Features.WebHost.Options;

namespace Blockcore.Features.WebHost
{
    public interface IClientEventBroadcaster
    {
        void Init(ClientEventBroadcasterSettings broadcasterSettings);
    }
}