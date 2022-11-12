using System.Collections.Generic;
using Blockcore.Consensus.Chain;
using NBitcoin;

namespace Blockcore.BlockPulling
{
    /// <summary>Represents consecutive collection of headers that are to be downloaded.</summary>
    public struct DownloadJob
    {

        public DownloadJob(int Id, List<ChainedHeader> Headers)
        {
            this.Id = Id;
            this.Headers = Headers;
        }
        /// <summary>Unique identifier of this job.</summary>
        private int Id;

        private List<ChainedHeader> Headers;


        public int getId() { return this.Id; }

        public List<ChainedHeader> getHeaders() { return this.Headers; }

    };

}





