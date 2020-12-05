using System;
using System.Collections.Generic;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.Consensus.Rules.ProvenHeaderRules;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Features.MemoryPool.Rules;
using Rutanio.Networks.Policies;
using Rutanio.Networks.Rules;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using System.Collections;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks;
using Blockcore.P2P;
using Rutanio.Networks.Consensus;
using Rutanio.Networks.Setup;
using Rutanio.Networks.Deployments;
using NBitcoin.Protocol;

namespace Rutanio.Networks
{
    public class RutanioMain : Network
    {
        public RutanioMain()
        {
            // START MODIFICATIONS OF GENERATED CODE
            var consensusOptions = new RutanioPosConsensusOptions
            {
                MaxBlockBaseSize = 1_000_000,
                MaxStandardVersion = 2,
                MaxStandardTxWeight = 100_000,
                MaxBlockSigopsCost = 20_000,
                MaxStandardTxSigopsCost = 20_000 / 5,
                WitnessScaleFactor = 4
            };
            // END MODIFICATIONS

            CoinSetup setup = RutanioSetup.Instance.Setup;
            NetworkSetup network = RutanioSetup.Instance.Main;

            NetworkType = NetworkType.Mainnet;
            DefaultConfigFilename = setup.ConfigFileName; // The default name used for the Rutanio configuration file.

            Name = network.Name;
            CoinTicker = network.CoinTicker;
            Magic = ConversionTools.ConvertToUInt32(setup.Magic);
            RootFolderName = network.RootFolderName;
            DefaultPort = network.DefaultPort;
            DefaultRPCPort = network.DefaultRPCPort;
            DefaultAPIPort = network.DefaultAPIPort;

            DefaultMaxOutboundConnections = 16;
            DefaultMaxInboundConnections = 109;
            MaxTipAge = 2 * 60 * 60;
            MinTxFee = 10000;
            MaxTxFee = Money.Coins(1).Satoshi;
            FallbackFee = 10000;
            MinRelayTxFee = 10000;
            MaxTimeOffsetSeconds = 25 * 60;
            DefaultBanTimeSeconds = 16000; // 500 (MaxReorg) * 64 (TargetSpacing) / 2 = 4 hours, 26 minutes and 40 seconds

            var consensusFactory = new PosConsensusFactory();

            GenesisTime = network.GenesisTime;
            GenesisNonce = network.GenesisNonce;
            GenesisBits = network.GenesisBits;
            GenesisVersion = network.GenesisVersion;
            GenesisReward = network.GenesisReward;

            Block genesisBlock = CreateGenesisBlock(consensusFactory,
               GenesisTime,
               GenesisNonce,
               GenesisBits,
               GenesisVersion,
               GenesisReward,
               setup.GenesisText);

            Genesis = genesisBlock;

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };

            var bip9Deployments = new RutanioBIP9Deployments
            {
                [RutanioBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 27, new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), BIP9DeploymentsParameters.DefaultMainnetThreshold),
                [RutanioBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0, new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), BIP9DeploymentsParameters.DefaultMainnetThreshold),
                [RutanioBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1, new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), BIP9DeploymentsParameters.DefaultMainnetThreshold)
            };

            consensusFactory.Protocol = new ConsensusProtocol()
            {
                ProtocolVersion = ProtocolVersion.FEEFILTER_VERSION,
                MinProtocolVersion = ProtocolVersion.POS_PROTOCOL_VERSION,
            };

            Consensus = new Blockcore.Consensus.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: setup.CoinType,
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
                maxMoney: long.MaxValue,
                coinbaseMaturity: 50,
                premineHeight: 2,
                premineReward: Money.Coins(setup.PremineReward),
                proofOfWorkReward: Money.Coins(setup.PoWBlockReward),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                targetSpacing: setup.TargetSpacing,
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: setup.LastPowBlock,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(setup.PoSBlockReward),
                proofOfStakeTimestampMask: setup.ProofOfStakeTimestampMask
            );

            // HashKey Definition
            byte[] newHashkey = Encoders.ASCII.DecodeData("Rutanio seed");
            ExtKey.OverrideHashKey(newHashkey);

            Consensus.PosEmptyCoinbase = RutanioSetup.Instance.IsPoSv3();
            Consensus.PosUseTimeFieldInKernalHash = RutanioSetup.Instance.IsPoSv3();

            // TODO: Set your Base58Prefixes
            Base58Prefixes = new byte[12][];
            Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
            Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
            Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (byte)network.SecretAddress };
            Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder(network.CoinTicker.ToLowerInvariant());
            Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            Checkpoints = network.Checkpoints;
            DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
            SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(Dns.GetHostAddresses(node).GetValue(0).ToString()), network.DefaultPort)).ToList();

            StandardScriptsRegistry = new RutanioStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(DefaultBanTimeSeconds <= Consensus.MaxReorgLength * 64 / 2);

            Assert(Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
            Assert(Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

            RegisterRules(Consensus);
            RegisterMempoolRules(Consensus);
        }

        protected void RegisterRules(IConsensus consensus)
        {
            consensus.ConsensusRules
                .Register<HeaderTimeChecksRule>()
                .Register<HeaderTimeChecksPosRule>()
                .Register<RutanioBugFixPosFutureDriftRule>()
                .Register<CheckDifficultyPosRule>()
                .Register<RutanioHeaderVersionRule>()
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

                /// rules that require the store to be loaded (coinview)
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