using System.IO;
using Blockcore.Consensus.BlockInfo;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.Crypto;
using Blockcore.Networks.XRC.Crypto;

namespace Blockcore.Networks.XRC.Consensus
{
    public class XRCBlockHeader : PosBlockHeader
    {
        private const int X13_HASH_MINERUNCOMPATIBLE = 1;
        private const int X13_HASH_MINERCOMPATIBLE = 2;

        public XRCConsensusProtocol Consensus { get; set; }

        public XRCBlockHeader(XRCConsensusProtocol consensus)
        {
            this.Consensus = consensus;
        }

        public override uint256 GetHash()
        {
            uint256 hash = null;
            uint256[] innerHashes = this.hashes;

            if (innerHashes != null)
                hash = innerHashes[0];

            if (hash != null)
                return hash;

            using (var hs = new HashStream())
            {
                this.ReadWriteHashingStream(new BitcoinStream(hs, true));
                hash = hs.GetHash();
            }

            innerHashes = this.hashes;
            if (innerHashes != null)
            {
                innerHashes[0] = hash;
            }

            return hash;
        }

        public override uint256 GetPoWHash()
        {
            using (var ms = new MemoryStream())
            {
                this.ReadWriteHashingStream(new BitcoinStream(ms, true));
                
                if (this.Time > this.Consensus.PowDigiShieldX11Time)
                {
                    return XRCHashX11.Instance.Hash(this.ToBytes());
                }
                //block HardFork - height: 1648, time - 1541879606, hash - a75312cab7cf2a6ee89ab33bcb0ab9f96676fbc965041d50b889d9469eff6cdb 
                else if (this.Time > this.Consensus.PowLimit2Time)
                {
                    return XRCHashX13.Instance.Hash(this.ToBytes(), X13_HASH_MINERCOMPATIBLE);
                }
                else
                {
                    return XRCHashX13.Instance.Hash(this.ToBytes(), X13_HASH_MINERUNCOMPATIBLE);
                }
            }
        }
    }
}
