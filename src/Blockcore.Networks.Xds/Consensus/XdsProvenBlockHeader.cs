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

        //public uint256 GetPoWHash()
        //{
        //    byte[] serialized;

        //    using (var ms = new MemoryStream())
        //    {
        //        this.PosBlockHeader.ReadWriteHashingStream(new BitcoinStream(ms, true));
        //        serialized = ms.ToArray();
        //    }

        //    return Sha512T.GetHash(serialized);
        //}
    }
}