using System.Collections.Generic;
using Blockcore.Consensus.Chain;
using NBitcoin;

namespace Blockcore.BlockPulling
{
    /// <summary>Represents consecutive collection of headers that are to be downloaded.</summary>
    public struct DownloadJob
    {
        /// <summary>Unique identifier of this job.</summary>
        private int id;

        public int Id { get { return this.id; } set { this.id = value; } }

        /// <summary>Headers of blocks that are to be downloaded.</summary>
        public List<ChainedHeader> Headers;
    }
}
