namespace Blockcore.Features.SignalR
{
    public interface IClientEventBroadcaster
    {
        void Init(ClientEventBroadcasterSettings broadcasterSettings);
    }
}