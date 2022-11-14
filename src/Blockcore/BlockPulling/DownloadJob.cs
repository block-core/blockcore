using System.Collections.Generic;
using Blockcore.Consensus.Chain;

namespace Blockcore.BlockPulling
{
    /// <summary>Represents consecutive collection of headers that are to be downloaded.</summary>
    public struct DownloadJob
    {

        public DownloadJob(int id, List<ChainedHeader> headers)
        {
            this.Id = id;
            this.Headers = headers;
        }

        public int Id { get; set; }

        public List<ChainedHeader> Headers;
    }
}
