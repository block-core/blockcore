using System.IO;
using Blockcore.Consensus.Block;
using NBitcoin;
using NBitcoin.Crypto;

namespace Blockcore.Networks.Xds.Consensus
{
    public class XdsProvenBlockHeader : ProvenBlockHeader
    {
        public XdsProvenBlockHeader()
        {
        }

        public XdsProvenBlockHeader(PosBlock block, XdsBlockHeader xdsBlockHeader) : base(block, xdsBlockHeader)
        {
        }
    }
}