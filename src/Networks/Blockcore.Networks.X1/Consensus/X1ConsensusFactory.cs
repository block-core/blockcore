using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.X1.Consensus
{
    public class X1ConsensusFactory : PosConsensusFactory
    {
        public override BlockHeader CreateBlockHeader()
        {
            return new X1BlockHeader();
        }

        public override ProvenBlockHeader CreateProvenBlockHeader()
        {
            return new X1ProvenBlockHeader();
        }

        public override ProvenBlockHeader CreateProvenBlockHeader(PosBlock block)
        {
            var provenBlockHeader = new X1ProvenBlockHeader(block, (X1BlockHeader)this.CreateBlockHeader());

            // Serialize the size.
            provenBlockHeader.ToBytes(this);

            return provenBlockHeader;
        }

        public override Transaction CreateTransaction()
        {
            return new X1Transaction();
        }

        public override Transaction CreateTransaction(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var transaction = new X1Transaction();
            transaction.ReadWrite(bytes, this);
            return transaction;
        }

        public override Transaction CreateTransaction(string hex)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));

            return CreateTransaction(Encoders.Hex.DecodeData(hex));
        }

        public Block ComputeGenesisBlock(uint genesisTime, uint genesisNonce, uint genesisBits, int genesisVersion, Money genesisReward, NetworkType networkType, bool? mine = false)
        {
            if (mine == true)
                MineGenesisBlock(genesisTime, genesisBits, genesisVersion, genesisReward, networkType);

            string pszTimestamp = "https://www.blockchain.com/btc/block/611000";

            Transaction txNew = CreateTransaction();
            Debug.Assert(txNew.GetType() == typeof(X1Transaction));

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
            {
                switch (networkType)
                {
                    case NetworkType.Mainnet:
                        if (genesis.GetHash() ==
                            uint256.Parse("0000000e13c5bf36c155c7cb1681053d607c191fc44b863d0c5aef6d27b8eb8f") &&
                            genesis.Header.HashMerkleRoot ==
                            uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
                            return genesis;
                        break;
                    case NetworkType.Testnet:
                        if (genesis.GetHash() ==
                            uint256.Parse("00000d2ff9f3620b5487ed8ec154ce1947fec525e91e6973d1aeae93c53db7a3") &&
                            genesis.Header.HashMerkleRoot ==
                            uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
                            return genesis;
                        break;
                    case NetworkType.Regtest:
                        if (genesis.GetHash() ==
                            uint256.Parse("00000e48aeeedabface6d45c0de52c7d0edaec14662ab4f56401361f70d12cc6") &&
                            genesis.Header.HashMerkleRoot ==
                            uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
                            return genesis;
                        break;
                }
            }
            else if (mine == null)
                return genesis;

            throw new InvalidOperationException($"Invalid {networkType}.");
        }

        private void MineGenesisBlock(uint genesisTime, uint genesisBits, int genesisVersion, Money genesisReward, NetworkType networkType)
        {
            Parallel.ForEach(new long[] { 0, 1, 2, 3, 4, 5, 6, 7 }, l =>
            {
                if (Utils.UnixTimeToDateTime(genesisTime) > DateTime.UtcNow)
                    throw new Exception("Time must not be in the future");
                uint nonce = 0;
                while (!ComputeGenesisBlock(genesisTime, nonce, genesisBits, genesisVersion, genesisReward, networkType, null).GetHash().ToString().StartsWith("00000000"))
                {
                    nonce += 8;
                }

                Block genesisBlock = ComputeGenesisBlock(genesisTime, nonce, genesisBits, genesisVersion, genesisReward, networkType, null);
                throw new Exception($"Found: Nonce:{nonce}, Hash: {genesisBlock.GetHash()}, Hash Merkle Root: {genesisBlock.Header.HashMerkleRoot}");
            });
        }
    }
}