using Blockcore.Primitives;
using NBitcoin;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// Event that is executed when a block is connected to a consensus chain.
    /// </summary>
    /// <seealso cref="EventBase" />
    public class BlockConnected : EventBase
    {
        public ChainedHeaderBlock ConnectedBlock { get; }

        public BlockConnected(ChainedHeaderBlock connectedBlock)
        {
            this.ConnectedBlock = connectedBlock;
        }
    }
}