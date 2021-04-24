using System;
using System.Collections.Generic;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.Consensus.Rules.ProvenHeaderRules;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Features.MemoryPool.Rules;
using Blockcore.Networks.SERF.Policies;
using Blockcore.Networks.SERF.Rules;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using System.Collections;
using System.Linq;
using System.Collections.Specialized;
using System.Net;
using Blockcore.Networks.SERF.Setup;
using Blockcore.Networks;
using Blockcore.Base.Deployments;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus;
using Blockcore.P2P;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Networks.SERF.Deployments;

namespace Blockcore.Networks.SERF
{
    public class SERFMain : Network
    {
        public SERFMain()
        {
            CoinSetup setup = SERFSetup.Instance.Setup;
            NetworkSetup network = SERFSetup.Instance.Main;

            this.NetworkType = NetworkType.Mainnet;
            this.DefaultConfigFilename = setup.ConfigFileName; // The default name used for the SERF configuration file.

            this.Name = network.Name;
            this.CoinTicker = network.CoinTicker;
            this.Magic = ConversionTools.ConvertToUInt32(setup.Magic);
            this.RootFolderName = network.RootFolderName;
            this.DefaultPort = network.DefaultPort;
            this.DefaultRPCPort = network.DefaultRPCPort;
            this.DefaultAPIPort = network.DefaultAPIPort;
            this.MaxTimeOffsetSeconds = BitcoinMaxTimeOffsetSeconds;
            this.MaxTipAge = BitcoinDefaultMaxTipAgeInSeconds;

            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            
            this.MinTxFee = 10000;
            this.MaxTxFee = Money.Coins(1).Satoshi;
            this.FallbackFee = 1000000;
            this.MinRelayTxFee = 10000;
            
            this.DefaultBanTimeSeconds = 16000; // 500 (MaxReorg) * 64 (TargetSpacing) / 2 = 4 hours, 26 minutes and 40 seconds

            var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = network.GenesisTime;
            this.GenesisNonce = network.GenesisNonce;
            this.GenesisBits = network.GenesisBits;
            this.GenesisVersion = network.GenesisVersion;
            this.GenesisReward = network.GenesisReward;

            Block genesisBlock = CreateGenesisBlock(consensusFactory,
            this.GenesisTime,
            this.GenesisNonce,
            this.GenesisBits,
            this.GenesisVersion,
            this.GenesisReward,
            setup.GenesisText);

            this.Genesis = genesisBlock;

            var consensusOptions = new PosConsensusOptions
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

            var bip9Deployments = new SERFBIP9Deployments()
            {
                [SERFBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.AlwaysActive),
                [SERFBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.AlwaysActive),
                [SERFBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 2, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.AlwaysActive),
            };

            this.Consensus = new Blockcore.Consensus.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: setup.CoinType,
                hashGenesisBlock: genesisBlock.GetHash(),
                subsidyHalvingInterval: 525600,
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
                targetTimespan: TimeSpan.FromSeconds(120), // two mins
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

            this.Consensus.PosEmptyCoinbase = SERFSetup.Instance.IsPoSv3();
            this.Consensus.PosUseTimeFieldInKernalHash = SERFSetup.Instance.IsPoSv3();

            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (byte)network.SecretAddress };

            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            this.Bech32Encoders = new Bech32Encoder[2];
         
            var encoder = new Bech32Encoder(network.CoinTicker.ToLowerInvariant());
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.Checkpoints = network.Checkpoints;
            this.DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
            this.SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(node), network.DefaultPort)).ToList();

            this.StandardScriptsRegistry = new SERFStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * 64 / 2);

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

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
                 .Register<SERFHeaderVersionRule>()
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

        /// <summary> Bitcoin maximal value for the calculated time offset. If the value is over this limit, the time syncing feature will be switched off. </summary>
        public const int BitcoinMaxTimeOffsetSeconds = 70 * 60;

        /// <summary> Bitcoin default value for the maximum tip age in seconds to consider the node in initial block download (24 hours). </summary>
        public const int BitcoinDefaultMaxTipAgeInSeconds = 24 * 60 * 60;

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
