﻿using Blockcore.Primitives;
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

        public uint256 Hash { get; set; }

        public int Height { get; set; }

        public BlockConnected(ChainedHeaderBlock connectedBlock)
        {
            this.ConnectedBlock = connectedBlock;
            this.Hash = this.ConnectedBlock.ChainedHeader.HashBlock;
            this.Height = this.ConnectedBlock.ChainedHeader.Height;
        }
    }
}