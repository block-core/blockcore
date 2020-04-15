using System.IO;
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