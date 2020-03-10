using Blockcore.Primitives;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// Event that is executed when a block is connected to a consensus chain.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class BlockConnected : EventBase
    {
        public ChainedHeaderBlock ConnectedBlock { get; }

        public BlockConnected(ChainedHeaderBlock connectedBlock)
        {
            this.ConnectedBlock = connectedBlock;
        }
    }
}