using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.BlockInfo;

namespace NBitcoin
{
    public class MerkleBlock : IBitcoinSerializable
    {
        public MerkleBlock()
        {
        }

        // Public only for unit testing
        private BlockHeader header;

        public BlockHeader Header
        {
            get
            {
                return this.header;
            }
            set
            {
                this.header = value;
            }
        }

        private PartialMerkleTree _PartialMerkleTree;

        public PartialMerkleTree PartialMerkleTree
        {
            get
            {
                return this._PartialMerkleTree;
            }
            set
            {
                this._PartialMerkleTree = value;
            }
        }

        public MerkleBlock(Block block, uint256[] txIds)
        {
            this.header = block.Header;

            var vMatch = new List<bool>();
            var vHashes = new List<uint256>();
            for (int i = 0; i < block.Transactions.Count; i++)
            {
                uint256 hash = block.Transactions[i].GetHash();
                vHashes.Add(hash);
                vMatch.Add(txIds.Contains(hash));
            }

            this._PartialMerkleTree = new PartialMerkleTree(vHashes.ToArray(), vMatch.ToArray());
        }

        #region IBitcoinSerializable Members

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.header);
            stream.ReadWrite(ref this._PartialMerkleTree);
        }

        #endregion IBitcoinSerializable Members
    }
}