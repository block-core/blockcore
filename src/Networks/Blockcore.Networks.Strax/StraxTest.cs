using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.Strax.Deployments;
using Blockcore.Networks.Strax.Federation;
using Blockcore.Networks.Strax.Policies;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace Blockcore.Networks.Strax
{
    public sealed class StraxTest : StraxBaseNetwork
    {
        public StraxTest()
        {
            this.Name = "StraxTest";
            this.NetworkType = NetworkType.Testnet;
            this.Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("TtrX"));
            this.DefaultPort = 27105;
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.DefaultRPCPort = 27104;
            this.DefaultAPIPort = 27103;
            //this.DefaultSignalRPort = 27102;
            this.MaxTipAge = 2 * 60 * 60;
            this.MinTxFee = 10000;
            this.MaxTxFee = Money.Coins(1).Satoshi;
            this.FallbackFee = 10000;
            this.MinRelayTxFee = 10000;
            this.RootFolderName = StraxNetwork.StraxRootFolderName;
            this.DefaultConfigFilename = StraxNetwork.StraxDefaultConfigFilename;
            this.MaxTimeOffsetSeconds = 25 * 60;
            this.CoinTicker = "TSTRAX";
            this.DefaultBanTimeSeconds = 11250; // 500 (MaxReorg) * 45 (TargetSpacing) / 2 = 3 hours, 7 minutes and 30 seconds

            this.CirrusRewardDummyAddress = "tGXZrZiU44fx3SQj8tAQ3Zexy2VuELZtoh";

            var consensusFactory = new StraxConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1598918400; // 1 September 2020
            this.GenesisNonce = 109534;
            this.GenesisBits = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000")).ToCompact(); // This should be set to the same as the PowLimit
            this.GenesisVersion = 1;
            this.GenesisReward = Money.Zero;

            Block genesisBlock = StraxNetwork.CreateGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward, "teststraxgenesisblock");

            this.Genesis = genesisBlock;

            // Taken from Stratis.
            var consensusOptions = new PosConsensusOptions
            {
                MaxBlockBaseSize = 1_000_000,
                MaxBlockSerializedSize = 4_000_000,
                MaxStandardVersion = 2,
                MaxStandardTxWeight = 150_000,
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

            var bip9Deployments = new StraxBIP9Deployments()
            {
                // Always active.
                [StraxBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultTestnetThreshold),
                [StraxBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultTestnetThreshold),
                [StraxBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 2, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultTestnetThreshold)
            };

            // To successfully process the OP_FEDERATION opcode the federations should be known.
            this.Federations = new Federations();

            // This should mirror the federation registered in CirrusTest.
            this.Federations.RegisterFederation(new Federation.Federation(new[] {
               new PubKey("021040ef28c82fcffb63028e69081605ed4712910c8384d5115c9ffeacd9dbcae4"),//Node1
               new PubKey("0244290a31824ba7d53e59c7a29d13dbeca15a9b0d36fdd4d28fce426753107bfc"),//Node2
               new PubKey("032df4a2d62c0db12cd1d66201819a10788637c9b90a1cd2a5a3f5196fdab7a621"),//Node3
               new PubKey("028ed190eb4ed6e46440ac6af21d8a67a537bd1bd7edb9cc5177d36d5a0972244d"),//Node4
               new PubKey("02ff9923324399a188daf4310825a85dd3b89e2301d0ad073295b6f33ae1c72f7a"),//Node5
               new PubKey("030e03b808ddb51701d4d3dbc0a74a6f9aedfecf23d5f874914641fc81197b239a"),//Node7
               new PubKey("02270d6c20d3393fad7f74c59d2d26b0824ed016ccbc15e698e7354314459a60a5"),//Node8
            }));

            this.Consensus = new Consensus.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: 1, // Per https://github.com/satoshilabs/slips/blob/master/slip-0044.md - testnets share a cointype
                hashGenesisBlock: genesisBlock.GetHash(),
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: buriedDeployments,
                bip9Deployments: bip9Deployments,
                bip34Hash: null,
                minerConfirmationWindow: 2016,
                maxReorgLength: 500,
                defaultAssumeValid: null,
                maxMoney: long.MaxValue,
                coinbaseMaturity: 50,
                premineHeight: 2,
                premineReward: Money.Coins(130000000),
                proofOfWorkReward: Money.Coins(18),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                targetSpacing: TimeSpan.FromSeconds(45),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 12500,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(18),
                proofOfStakeTimestampMask: 0x0000000F // 16 sec
            );

            this.Consensus.PosEmptyCoinbase = false;

            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { 120 }; // q
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { 127 }; // t
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { 120 + 128 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { 0x04, 0x88, 0xB2, 0x1E };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { 0x04, 0x88, 0xAD, 0xE4 };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x0000db68ff9e74fbaf7654bab4fa702c237318428fa9186055c243ddde6354ca"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                { 2, new CheckpointInfo(new uint256("0xe334d9b91f59eb61b02695358816598320d739c65154fa5d2bf9c7bf6b69384f"), new uint256("0x9e104e64e598dfa8287daf15259a694d057b24adf2dd63dab861015397497c88")) }, // Premine
                { 100, new CheckpointInfo(new uint256("0x10f4c147c0d8fccbf35b56a9951966fc0d7c0281e44100b0c948af7f582fb1da"), new uint256("0x6e30665551970eb3f12f849a7c841e7db262d3118be684102f9ababf775f1dfc")) },
                { 10_000, new CheckpointInfo(new uint256("0xe7885b91de04dfb65255712277839f9dc4b364346a838ad00fdb4de40825c075"), new uint256("0xe9c91960b4bf6efb3e7c56e6956eab75cd89b14cb13061130be2de70d0b49ac2")) },
                { 50_000, new CheckpointInfo(new uint256("0x1fe9bea56c58da86c262667c654ac2a951a07b50816ca5358b472a7961257abe"), new uint256("0x1a75a70969eeb668c45452080878e7bb95d232596e0b67293cab8d80a67ce7d3")) },
                { 100_000, new CheckpointInfo(new uint256("0x400ccde5f1c840805b5840eb744871605e0bbca9c3a997f977a5e4e8f21dc264"), new uint256("0x0d08a9f68ee4d1ef8387ab6afc1ab0810a0085f900db3361c239903828a4bba0")) },
                { 250_000, new CheckpointInfo(new uint256("0x60752fc5cf4e326e4b7fa44992affa30abb5dfdd52680f84db390d158237e24d"), new uint256("0x6f07cc332ef049b2f7fd06ff0c5883830c85d9e5ca965c39a3a2c97cf1bfe92d")) },
                { 350_000, new CheckpointInfo(new uint256("0xc409b84bfd525550b535c64ca4d1becb1663b369e86c0d8af5b346b3b7f951b8"), new uint256("0x1dc8e7fd11a833a722c0b0c48db8a5eec10074fbf3066618e09e5662f6ff2113")) },
                { 500_000, new CheckpointInfo(new uint256("0xda5da5c0ac8f34e89d6d308e1a046e98e46080941670e327d9eb84dc859d153f"), new uint256("0x1f73717627345bdc6d7b9b521dcea85df2586208a6d3a90fcd2efd16dcf9c591")) },
                { 650_000, new CheckpointInfo(new uint256("0x50b2ddb88c5efe942d8bf6a07bed996f44b3b663df0f77d5d88ad1adba48329b"), new uint256("0xb507c86a412b9e50d0bed3be52a9042c2dbaca6653ff6ccb3e2e355c24c73a70")) },
                { 750_000, new CheckpointInfo(new uint256("0x592842f3e5af517b0ce6f451f6b61738a6dea1007ccbaab39f22878de8de78dc"), new uint256("0x6ee053737f80a3a5173c10a507b1d1ea2ec9f6fa6be07b2b9d26558e4622f4a4")) },
            };

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder("tstrax");
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("testnet1.stratisnetwork.com", "testnet1.stratisnetwork.com")
            };

            this.SeedNodes = new List<NetworkAddress>
            {
                new NetworkAddress(IPAddress.Parse("82.146.153.140"), 27105), // Iain
            };

            this.StandardScriptsRegistry = new StraxStandardScriptsRegistry();

            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * this.Consensus.TargetSpacing.TotalSeconds / 2);

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("0x0000db68ff9e74fbaf7654bab4fa702c237318428fa9186055c243ddde6354ca"));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("0xfe6317d42149b091399e7f834ca32fd248f8f26f493c30a35d6eea692fe4fcad"));

            StraxNetwork.RegisterRules(this.Consensus);
            StraxNetwork.RegisterMempoolRules(this.Consensus);
        }
    }
}
