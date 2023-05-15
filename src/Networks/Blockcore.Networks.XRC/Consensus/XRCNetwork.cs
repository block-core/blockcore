using System;
using System.Collections.Generic;
using Blockcore.Consensus;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Features.MemoryPool.Rules;
using Blockcore.Networks.XRC.Rules;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.DataEncoders;

namespace Blockcore.Networks.XRC.Consensus
{
    public class XRCNetwork : Network
    {
        public Block CreateXRCGenesisBlock(XRCConsensusFactory consensusFactory, uint nTime, uint nNonce, uint nBits, int nVersion, string pubKey)
        {
            string message = "Release the Kraken!!! Zeus";
            return CreateXRCGenesisBlock(consensusFactory, message, nTime, nNonce, nBits, nVersion, pubKey);
        }

        private Block CreateXRCGenesisBlock(XRCConsensusFactory consensusFactory, string message, uint nTime, uint nNonce, uint nBits, int nVersion, string pubKey)
        {
            //nTime = 1512043200 => Thursday, November 30, 2017 12:00:00 PM (born XRC)
            //nTime = 1527811200 => Friday, Jun 1, 2017 12:00:00 PM (born TestXRC)
            //nBits = 0x1d00ffff (it is exactly 0x1b = 27 bytes long) => 0x00ffff0000000000000000000000000000000000000000000000000000 => 1
            //nNonce = XTimes to trying to find a genesis block
            Transaction txNew = consensusFactory.CreateTransaction();
            txNew.Version = 2;
            if (txNew is IPosTransactionWithTime posTx)
                posTx.Time = nTime;
            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(nBits), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)4 }
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(message)))
            });
            txNew.AddOutput(new TxOut()
            {
                Value = Money.Zero,
                ScriptPubKey = Script.FromBytesUnsafe(Encoders.Hex.DecodeData(pubKey))
            });

            Block genesis = consensusFactory.CreateBlock();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
            genesis.Header.Bits = nBits;
            genesis.Header.Nonce = nNonce;
            genesis.Header.Version = nVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();
            return genesis;
        }

        protected void RegisterRules(IConsensus consensus)
        {
            consensus.ConsensusRules
                .Register<HeaderTimeChecksRule>()
                .Register<XRCCheckDifficultyPowRule>()
                .Register<XRCHeaderVersionRule>();

            consensus.ConsensusRules
                .Register<BlockMerkleRootRule>();

            consensus.ConsensusRules
                .Register<SetActivationDeploymentsPartialValidationRule>()

                .Register<TransactionLocktimeActivationRule>()
                .Register<CoinbaseHeightActivationRule>()
                .Register<WitnessCommitmentsRule>()
                .Register<BlockSizeRule>()

                .Register<EnsureCoinbaseRule>()
                .Register<CheckPowTransactionRule>()
                .Register<CheckSigOpsRule>();

            consensus.ConsensusRules
                .Register<SetActivationDeploymentsFullValidationRule>()

                // rules that require the store to be loaded (coinview)
                .Register<FetchUtxosetRule>()
                .Register<TransactionDuplicationActivationRule>()
                .Register<CheckPowUtxosetPowRule>()
                .Register<PushUtxosetRule>()
                .Register<FlushUtxosetRule>();
        }

        protected void RegisterMempoolRules(IConsensus consensus)
        {
            consensus.MempoolRules = new List<Type>()
            {
                typeof(CheckConflictsMempoolRule),
                typeof(CheckCoinViewMempoolRule),
                typeof(CreateMempoolEntryMempoolRule),
                typeof(CheckSigOpsMempoolRule),
                typeof(CheckFeeMempoolRule),
                typeof(CheckRateLimitMempoolRule),
                typeof(CheckAncestorsMempoolRule),
                typeof(CheckReplacementMempoolRule),
                typeof(CheckAllInputsMempoolRule),
                typeof(CheckTxOutDustRule)
            };
        }
    }
}
