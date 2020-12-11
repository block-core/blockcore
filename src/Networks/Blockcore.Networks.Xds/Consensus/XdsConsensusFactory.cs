using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.Xds.Consensus
{
    public class XdsConsensusFactory : PosConsensusFactory
    {
        public override BlockHeader CreateBlockHeader()
        {
            return new XdsBlockHeader();
        }

        public override ProvenBlockHeader CreateProvenBlockHeader()
        {
            return new XdsProvenBlockHeader();
        }

        public override ProvenBlockHeader CreateProvenBlockHeader(PosBlock block)
        {
            var provenBlockHeader = new XdsProvenBlockHeader(block, (XdsBlockHeader)this.CreateBlockHeader());

            // Serialize the size.
            provenBlockHeader.ToBytes(this);

            return provenBlockHeader;
        }

        public override Transaction CreateTransaction()
        {
            return new XdsTransaction();
        }

        public override Transaction CreateTransaction(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var transaction = new XdsTransaction();
            transaction.ReadWrite(bytes, this);
            return transaction;
        }

        public override Transaction CreateTransaction(string hex)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));

            return CreateTransaction(Encoders.Hex.DecodeData(hex));
        }

        public Block ComputeGenesisBlock(uint genesisTime, uint genesisNonce, uint genesisBits, int genesisVersion, Money genesisReward, bool? mine = false)
        {
            if (mine == true)
                MineGenesisBlock(genesisTime, genesisBits, genesisVersion, genesisReward);

            string pszTimestamp = "https://www.blockchain.com/btc/block/611000";

            Transaction txNew = CreateTransaction();
            Debug.Assert(txNew.GetType() == typeof(XdsTransaction));

            txNew.Version = 1;

            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)42 }
                }, Op.GetPushOp(Encoding.UTF8.GetBytes(pszTimestamp)))
            });
            txNew.AddOutput(new TxOut()
            {
                Value = genesisReward,
            });
            Block genesis = CreateBlock();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(genesisTime);
            genesis.Header.Bits = genesisBits;
            genesis.Header.Nonce = genesisNonce;
            genesis.Header.Version = genesisVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();

            if (mine == false)
                if (genesis.GetHash() != uint256.Parse("0000000e13c5bf36c155c7cb1681053d607c191fc44b863d0c5aef6d27b8eb8f") ||
                    genesis.Header.HashMerkleRoot != uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
                    throw new InvalidOperationException("Invalid network");
            return genesis;
        }

        private void MineGenesisBlock(uint genesisTime, uint genesisBits, int genesisVersion, Money genesisReward)
        {
            Parallel.ForEach(new long[] { 0, 1, 2, 3, 4, 5, 6, 7 }, l =>
            {
                if (Utils.UnixTimeToDateTime(genesisTime) > DateTime.UtcNow)
                    throw new Exception("Time must not be in the future");
                uint nonce = 0;
                while (!ComputeGenesisBlock(genesisTime, nonce, genesisBits, genesisVersion, genesisReward, null).GetHash().ToString().StartsWith("00000000"))
                {
                    nonce += 8;
                }

                Block genesisBlock = ComputeGenesisBlock(genesisTime, nonce, genesisBits, genesisVersion, genesisReward, null);
                throw new Exception($"Found: Nonce:{nonce}, Hash: {genesisBlock.GetHash()}, Hash Merkle Root: {genesisBlock.Header.HashMerkleRoot}");
            });
        }
    }
}