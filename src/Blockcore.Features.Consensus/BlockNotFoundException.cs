using System;

namespace Blockcore.Features.Consensus
{
    public class BlockNotFoundException : Exception
    {
        public BlockNotFoundException(string message) : base(message)
        {
        }
    }
}
