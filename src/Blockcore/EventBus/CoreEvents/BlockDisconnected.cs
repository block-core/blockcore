using Blockcore.Primitives;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// Event that is executed when a block is disconnected from a consensus chain.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class BlockDisconnected : EventBase
    {
        public ChainedHeaderBlock DisconnectedBlock { get; }

        public BlockDisconnected(ChainedHeaderBlock disconnectedBlock)
        {
            this.DisconnectedBlock = disconnectedBlock;
        }
    }
}