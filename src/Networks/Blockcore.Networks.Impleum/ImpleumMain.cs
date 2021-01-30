using System;
using System.Collections.Generic;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.Consensus.Rules.ProvenHeaderRules;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Features.MemoryPool.Rules;
using Blockcore.Networks.Impleum.Deployments;
using Blockcore.Networks.Impleum.Policies;
using Blockcore.Networks.Impleum.Rules;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.Impleum
{
    public class ImpleumMain : Network
    {
        public ImpleumMain()
        {
            this.NetworkType = NetworkType.Mainnet;
            this.DefaultConfigFilename = ImpleumSetup.ConfigFileName; // The default name used for the Impleum configuration file.

            this.Name = ImpleumSetup.Main.Name;
            this.CoinTicker = ImpleumSetup.Main.CoinTicker;
            this.Magic = ImpleumSetup.Main.Magic;
            this.RootFolderName = ImpleumSetup.Main.RootFolderName;
            this.DefaultPort = ImpleumSetup.Main.DefaultPort;
            this.DefaultRPCPort = ImpleumSetup.Main.DefaultRPCPort;
            this.DefaultAPIPort = ImpleumSetup.Main.DefaultAPIPort;
            
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.MaxTipAge = 2 * 60 * 60;
            this.MinTxFee = 10000;
            this.MaxTxFee = Money.Coins(0.1m);
            this.FallbackFee = 10000;
            this.MinRelayTxFee = 10000;
            this.MaxTimeOffsetSeconds = 25 * 60;
            this.DefaultBanTimeSeconds = 11250; // 500 (MaxReorg) * 45 (TargetSpacing) / 2 = 3 hours, 7 minutes and 30 seconds

            var consensusFactory = new PosConsensusFactory();

            Block genesisBlock = CreateGenesisBlock(consensusFactory,
               ImpleumSetup.Main.GenesisTime,
               ImpleumSetup.Main.GenesisNonce,
               ImpleumSetup.Main.GenesisBits,
               ImpleumSetup.Main.GenesisVersion,
               ImpleumSetup.Main.GenesisReward,
               ImpleumSetup.GenesisText);

            this.Genesis = genesisBlock;
            
            // Taken from StratisX.
            var consensusOptions = new PosConsensusOptions()
            {
                MaxBlockBaseSize = 1_000_000,
                MaxStandardVersion = 2,
                MaxStandardTxWeight = 100_000,
                MaxBlockSigopsCost = 20_000,
                MaxStandardTxSigopsCost = 20_000 / 5,
                WitnessScaleFactor = 4
            };

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };

            var bip9Deployments = new ImpleumBIP9Deployments()
            {
                // Always active.
                [ImpleumBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultMainnetThreshold),
                [ImpleumBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultMainnetThreshold),
                [ImpleumBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 2, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultMainnetThreshold)
            };

            this.Consensus = new Consensus.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: ImpleumSetup.CoinType,
                hashGenesisBlock: genesisBlock.GetHash(),
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: buriedDeployments,
                bip9Deployments: bip9Deployments,
                bip34Hash: null,
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
                maxReorgLength: 500,
                defaultAssumeValid: null,
                maxMoney: Money.Coins(ImpleumSetup.MaxSupply),
                coinbaseMaturity: 50,
                premineHeight: 2,
                premineReward: Money.Coins(ImpleumSetup.PremineReward),
                proofOfWorkReward: Money.Coins(ImpleumSetup.PoWBlockReward),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                targetSpacing: ImpleumSetup.TargetSpacing,
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: ImpleumSetup.Main.LastPowBlock,
                proofOfStakeLimit: new BigInteger(uint256
                    .Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256
                    .Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(ImpleumSetup.PoSBlockReward),
                proofOfStakeTimestampMask: ImpleumSetup.ProofOfStakeTimestampMask
            )
            {
                PosEmptyCoinbase = ImpleumSetup.IsPoSv3(),
                PosUseTimeFieldInKernalHash = ImpleumSetup.IsPoSv3()
            };


            // TODO: Set your Base58Prefixes
            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (ImpleumSetup.Main.PubKeyAddress) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (ImpleumSetup.Main.ScriptAddress) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (ImpleumSetup.Main.SecretAddress) };

            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder(ImpleumSetup.Main.CoinTicker.ToLowerInvariant());
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.Checkpoints = ImpleumSetup.Main.Checkpoints;
            this.DNSSeeds = ImpleumSetup.Main.DNS;
            this.SeedNodes = ImpleumSetup.Main.Nodes;

            this.StandardScriptsRegistry = new ImpleumStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * 64 / 2);

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse(ImpleumSetup.Main.HashGenesisBlock));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse(ImpleumSetup.Main.HashMerkleRoot));

            RegisterRules(this.Consensus);
            RegisterMempoolRules(this.Consensus);
        }

        protected void RegisterRules(IConsensus consensus)
        {
            consensus.ConsensusRules
                .Register<HeaderTimeChecksRule>()
                .Register<HeaderTimeChecksPosRule>()
                .Register<PosFutureDriftRule>()
                .Register<CheckDifficultyPosRule>()
                .Register<ImpleumHeaderVersionRule>()
                .Register<ProvenHeaderSizeRule>()
                .Register<ProvenHeaderCoinstakeRule>();

            consensus.ConsensusRules
                .Register<BlockMerkleRootRule>()
                .Register<PosBlockSignatureRepresentationRule>()
                .Register<PosBlockSignatureRule>();

            consensus.ConsensusRules
                .Register<SetActivationDeploymentsPartialValidationRule>()
                .Register<PosTimeMaskRule>()

                // rules that are inside the method ContextualCheckBlock
                .Register<TransactionLocktimeActivationRule>()
                .Register<CoinbaseHeightActivationRule>()
                .Register<WitnessCommitmentsRule>()
                .Register<BlockSizeRule>()

                // rules that are inside the method CheckBlock
                .Register<EnsureCoinbaseRule>()
                .Register<CheckPowTransactionRule>()
                .Register<CheckPosTransactionRule>()
                .Register<CheckSigOpsRule>()
                .Register<PosCoinstakeRule>();

            consensus.ConsensusRules
                .Register<SetActivationDeploymentsFullValidationRule>()

                .Register<CheckDifficultyHybridRule>()

                // rules that require the store to be loaded (coinview)
                .Register<FetchUtxosetRule>()
                .Register<TransactionDuplicationActivationRule>()
                .Register<CheckPosUtxosetRule>() // implements BIP68, MaxSigOps and BlockReward calculation
                                                 // Place the PosColdStakingRule after the PosCoinviewRule to ensure that all input scripts have been evaluated
                                                 // and that the "IsColdCoinStake" flag would have been set by the OP_CHECKCOLDSTAKEVERIFY opcode if applicable.
                .Register<PosColdStakingRule>()
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

        protected static Block CreateGenesisBlock(ConsensusFactory consensusFactory, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward, string genesisText)
        {
            Transaction txNew = consensusFactory.CreateTransaction();
            txNew.Version = 1;

            if (txNew is IPosTransactionWithTime posTx)
            {
                posTx.Time = nTime;
            }

            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)42 }
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(genesisText)))
            });

            txNew.AddOutput(new TxOut()
            {
                Value = genesisReward,
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
    }
}
