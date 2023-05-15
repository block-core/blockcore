using System.IO;
using Blockcore.Consensus.BlockInfo;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.Crypto;

namespace Blockcore.Networks.Xds.Consensus
{
    public class XdsBlockHeader : PosBlockHeader
    {
        public override uint256 GetPoWHash()
        {
            byte[] serialized;

            using (var ms = new MemoryStream())
            {
                this.ReadWriteHashingStream(new BitcoinStream(ms, true));
                serialized = ms.ToArray();
            }

            return Sha512T.GetHash(serialized);
        }
    }
}