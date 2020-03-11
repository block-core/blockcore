using System;

namespace Blockcore.Features.Miner
{
    public class MinerException : Exception
    {
        public MinerException(string message) : base(message)
        {
        }
    }
}
