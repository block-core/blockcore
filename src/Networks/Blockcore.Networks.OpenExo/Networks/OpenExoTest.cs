using System;
using System.Linq;
using System.Net;
using Blockcore.Base.Deployments;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Networks;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using OpenExo.Networks.Consensus;
using OpenExo.Networks.Deployments;
using OpenExo.Networks.Policies;
using OpenExo.Networks.Setup;

namespace OpenExo.Networks
{
    public class OpenExoTest : OpenExoMain
    {
        public OpenExoTest()
        {
            // START MODIFICATIONS OF GENERATED CODE
            var consensusOptions = new OpenExoPosConsensusOptions
            {
                MaxBlockBaseSize = 1_000_000,
                MaxStandardVersion = 2,
                MaxStandardTxWeight = 100_000,
                MaxBlockSigopsCost = 20_000,
                MaxStandardTxSigopsCost = 20_000 / 5,
                WitnessScaleFactor = 4
            };
            // END MODIFICATIONS

            CoinSetup setup = OpenExoSetup.Instance.Setup;
            NetworkSetup network = OpenExoSetup.Instance.Test;

            this.NetworkType = NetworkType.Testnet;

            this.Name = network.Name;
            this.CoinTicker = network.CoinTicker;
            this.Magic = ConversionTools.ConvertToUInt32(setup.Magic, true);
            this.RootFolderName = network.RootFolderName;
            this.DefaultPort = network.DefaultPort;
            this.DefaultRPCPort = network.DefaultRPCPort;
            this.DefaultAPIPort = network.DefaultAPIPort;

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

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };

            var bip9Deployments = new OpenExoBIP9Deployments()
            {
                [OpenExoBIP9Deployments.TestDummy] = new BIP9DeploymentsParameters("TestDummy", 28,
                    new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    BIP9DeploymentsParameters.DefaultTestnetThreshold),

                [OpenExoBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0,
                    new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    BIP9DeploymentsParameters.DefaultTestnetThreshold),

                [OpenExoBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1,
                    new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    BIP9DeploymentsParameters.DefaultTestnetThreshold),

                [OpenExoBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 2,
                    new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    BIP9DeploymentsParameters.DefaultTestnetThreshold)
            };

            this.Consensus = new Blockcore.Consensus.Consensus(
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
                coinbaseMaturity: 10,
                premineHeight: 2,
                premineReward: Money.Coins(setup.PremineReward),
                proofOfWorkReward: Money.Coins(setup.PoWBlockReward),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                targetSpacing: setup.TargetSpacing,
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: setup.LastPowBlock,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(setup.PoSBlockReward),
                proofOfStakeTimestampMask: setup.ProofOfStakeTimestampMask
            );

            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (byte)network.SecretAddress };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { 0x04, 0x35, 0x87, 0xCF };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { 0x04, 0x35, 0x83, 0x94 };

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder(network.CoinTicker.ToLowerInvariant());
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.Checkpoints = network.Checkpoints;
            this.DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
            this.SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(Dns.GetHostAddresses(node).GetValue(0).ToString()), network.DefaultPort)).ToList();

            this.StandardScriptsRegistry = new OpenExoStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * 64 / 2);

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

            RegisterRules(this.Consensus);
            RegisterMempoolRules(this.Consensus);
        }
    }
}
