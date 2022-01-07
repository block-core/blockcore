using System.IO;
using Blockcore.Consensus.BlockInfo;
using NBitcoin;
using NBitcoin.Crypto;

namespace Blockcore.Networks.X1.Consensus
{
    public class X1BlockHeader : PosBlockHeader
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