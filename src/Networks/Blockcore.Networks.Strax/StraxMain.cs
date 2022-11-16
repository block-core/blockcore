using System;
using System.Collections.Generic;
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
    public class StraxMain : StraxBaseNetwork
    {
        public StraxMain()
        {
            this.Name = "StraxMain";
            this.NetworkType = NetworkType.Mainnet;
            this.Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("StrX"));
            this.DefaultPort = 17105;
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.DefaultRPCPort = 17104;
            this.DefaultAPIPort = 17103;
            //this.DefaultSignalRPort = 17102;
            this.MaxTipAge = 2 * 60 * 60;
            this.MinTxFee = 10000;
            this.MaxTxFee = Money.Coins(1).Satoshi;
            this.FallbackFee = 10000;
            this.MinRelayTxFee = 10000;
            this.RootFolderName = StraxNetwork.StraxRootFolderName;
            this.DefaultConfigFilename = StraxNetwork.StraxDefaultConfigFilename;
            this.MaxTimeOffsetSeconds = 25 * 60;
            this.CoinTicker = "STRAX";
            this.DefaultBanTimeSeconds = 11250; // 500 (MaxReorg) * 45 (TargetSpacing) / 2 = 3 hours, 7 minutes and 30 seconds

            this.CirrusRewardDummyAddress = "CPqxvnzfXngDi75xBJKqi4e6YrFsinrJka"; // Cirrus main address

            // To successfully process the OP_FEDERATION opcode the federations should be known.
            this.Federations = new Federations();
            this.Federations.RegisterFederation(new Federation.Federation(new[]
            {
                new PubKey("03797a2047f84ba7dcdd2816d4feba45ae70a59b3aa97f46f7877df61aa9f06a21"),
                new PubKey("0209cfca2490dec022f097114090c919e85047de0790c1c97451e0f50c2199a957"),
                new PubKey("032e4088451c5a7952fb6a862cdad27ea18b2e12bccb718f13c9fdcc1caf0535b4"),
                new PubKey("035bf78614171397b080c5b375dbb7a5ed2a4e6fb43a69083267c880f66de5a4f9"),
                new PubKey("02387a219b1de54d4dc73a710a2315d957fc37ab04052a6e225c89205b90a881cd"),
                new PubKey("028078c0613033e5b4d4745300ede15d87ed339e379daadc6481d87abcb78732fa"),
                new PubKey("02b3e16d2e4bbad6dba1e699934a52d58d9b60b6e7eed303e400e95f2dbc2ef3fd"),
                new PubKey("02ba8b842997ce50c8e29c24a5452de5482f1584ae79778950b7bae24d4cc68dad"),
                new PubKey("02cbd907b0bf4d757dee7ea4c28e63e46af19dc8df0c924ee5570d9457be2f4c73"),
                new PubKey("02d371f3a0cffffcf5636e6d4b79d9f018a1a18fbf64c39542b382c622b19af9de"),
                new PubKey("02f891910d28fc26f272da8d7f548fdc18c286704907673e839dc07e8df416c15e"),
                new PubKey("0337e816a3433c71c4bbc095a54a0715a6da7a70526d2afb8dba3d8d78d33053bf"),
                new PubKey("035569e42835e25c854daa7de77c20f1009119a5667494664a46b5154db7ee768a"),
                new PubKey("03cda7ea577e8fbe5d45b851910ec4a795e5cc12d498cf80d39ba1d9a455942188"),
                new PubKey("02680321118bce869933b07ea42cc04d2a2804134b06db582427d6b9688b3536a4")}));

            var consensusFactory = new StraxConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1604913812; // ~9 November 2020 - https://www.unixtimestamp.com/
            this.GenesisNonce = 747342; // Set to 1 until correct value found
            this.GenesisBits = 0x1e0fffff; // The difficulty target
            this.GenesisVersion = 536870912; // 'Empty' BIP9 deployments as they are all activated from genesis already
            this.GenesisReward = Money.Zero;

            Block genesisBlock = StraxNetwork.CreateGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, this.GenesisReward, "stratisplatform.com/2020/09/25/introducing-strax/");

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
                [StraxBIP9Deployments.CSV] = new BIP9DeploymentsParameters("CSV", 0, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultMainnetThreshold),
                [StraxBIP9Deployments.Segwit] = new BIP9DeploymentsParameters("Segwit", 1, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultMainnetThreshold),
                [StraxBIP9Deployments.ColdStaking] = new BIP9DeploymentsParameters("ColdStaking", 2, BIP9DeploymentsParameters.AlwaysActive, 999999999, BIP9DeploymentsParameters.DefaultMainnetThreshold)
            };

            this.Consensus = new Consensus.Consensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: 105105, // https://github.com/satoshilabs/slips/blob/master/slip-0044.md
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
                defaultAssumeValid: null, // TODO: Set this once some checkpoint candidates have elapsed
                maxMoney: long.MaxValue,
                coinbaseMaturity: 50,
                premineHeight: 2,
                premineReward: Money.Coins(124987850),
                proofOfWorkReward: Money.Coins(18),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60),
                targetSpacing: TimeSpan.FromSeconds(45),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: false,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 675,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.Coins(18),
                proofOfStakeTimestampMask: 0x0000000F // 16 sec
            );

            this.Consensus.PosEmptyCoinbase = false;

            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { 75 }; // X
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { 140 }; // y
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { 75 + 128 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { 0x04, 0x88, 0xB2, 0x1E };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { 0x04, 0x88, 0xAD, 0xE4 };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0xebe158d09325c470276619ebc5f7f87c98c0ed4b211c46a17a6457655811d082"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                { 2, new CheckpointInfo(new uint256("0xbaf2cff34756724b211c6adbe77eb22b3c7896a2773b89a97dd5fc2ddf28d6c2"), new uint256("0xe3ee3d02e12d8819a01f66ac67a7a41547cd5d19a5bc7392bf415308c6960aab")) }, // Premine
                { 50, new CheckpointInfo(new uint256("0x1ad414cd58cb74fc8c41fcfb0f207aebd3d5fed82f59609746779c39168adedc"), new uint256("0x0aa31c4bac4bdea08a65a08b579f210d8df28a15005a65c3a8edfc6f6428038e")) },
                { 70, new CheckpointInfo(new uint256("0x7ec7a9cd1ee45ff140bc4c0c1becc6809916b99fac3fae42e6e2c8e70d987259"), new uint256("0x6efbb2d43e885cbc90dc7dfe2b781fc6a4d8a64bd77201de593d25307a7401ce")) },
                { 100, new CheckpointInfo(new uint256("0x00bc48e8eae5b053e0d48b42997d637e8b36816c311339eb7e36c6eaca6f4674"), new uint256("0x0ad1e1ea9c0b8c90ff67b286cee07f25c9f1552b3749461e66c3ed2f4cd18d54")) },
                { 500, new CheckpointInfo(new uint256("0xf88ead17d09223d914e5bdca27f27ff861c4c1e5bfb4ab3bc4a4627d4ddd442a"), new uint256("0xf6ea71dcc38a4dbf6c66dd1d3e54568269d6f7cd411c15304de6fce5394a9702")) },
                { 800, new CheckpointInfo(new uint256("0x3bb02167ae32be4915c1a78150d5fc7ed9924be00c57c6e9693d7603d94970dc"), new uint256("0x995ff9e10f2ef8cddae35a9c40237bdd48fd7bc9982955d9aac50ee32d69d097")) },
                { 1500, new CheckpointInfo(new uint256("0x0d28f45849a2e535a7ef72690fc49fc9a5c101494c5d5753fdeb1ac121ffb120"), new uint256("0x4a8a47deb226784606625bc4fd545bfaf5aa314c4a20076185cb86e524a36173")) },
                { 10000, new CheckpointInfo(new uint256("0xc1c4b7d64b6669d32493c0ed08b42a8f193e07590a95f76a9212a86dc66057f0"), new uint256("0x10130e76efbf5f4cf093594430ea0d160728a3f3d6b5926887aaf7b674ad65b1")) },
                { 20000, new CheckpointInfo(new uint256("0xeb493db4b643a3cfbc5912bcda5296532ff1bccbdcb320b863698c63ecfea174"), new uint256("0xb35462fd7f43709f59efb0e7225d961c2e8a7569fc44102db5c99400ba9ad6bd")) },
                { 30000, new CheckpointInfo(new uint256("0x4d0f2a809ef915721ced21f5ec51b6177b684eee06cadd49bcedc57daa243b8b"), new uint256("0xd9b7f8c92f289d66cb35a517e0b5c11c3e7e23a6507ce8ba2f042642849dcba0")) },
                { 40000, new CheckpointInfo(new uint256("0xdc10671e67350eda9518b220e329ca9f661cd98c0e12d246471f8ec4f8a81c71"), new uint256("0xeb13622df7b0fc95068c0146d718bb2eaf2fd8943b3bea89396d8d58f5af8c15")) },
                { 50000, new CheckpointInfo(new uint256("0xe3398765bc0da5b481a5dfe60f0acf14f4b1fc8582bab8f7a166317aea9aa026"), new uint256("0x350db25ca3ff01ec589681c94c325f619e5013bdc06efcbefa981776f4dcca4f")) },
                { 60000, new CheckpointInfo(new uint256("0x9cbc20fd1720529c59073ade6f5511ab5c2cf168556c9a10cb41ff9d8dac724f"), new uint256("0xe363394313d2e1af248a1c0d18b79e6074a08884dddbebfca90e8ae716edb645")) },
                { 150_000, new CheckpointInfo(new uint256("0x48bb4c2f08088da9990e23f19cb4b9a094bdf7791f86f77a98d08e5d2b06c1ce"), new uint256("0x14f80d627e7727f4da4a5945ddb77e2821369246c72f1c6ca754c6509a4eef60"))},
                { 300_000, new CheckpointInfo(new uint256("0x35cb635c4f286b233fab6252c30f3df7813c0a76ca7ea2a90249cad73958e2d3"), new uint256("0x42e5a29b035296e3dee4f675f92c5790e0ac6cd0c9390fcf6bac9ac28ccaa850")) },
                { 450_000, new CheckpointInfo(new uint256("0xc08db6151e2f341360a28e6a796d9c4356e14085e81aed2338c05f1964ef3e27"), new uint256("0x0cfc40a07819297a39be5460f805ce391d7f9b8d5794b18c97384a6b832deb4b")) }
            };

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder("strax");
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("straxmainnet1.stratisnetwork.com", "straxmainnet1.stratisnetwork.com"),
                new DNSSeedData("straxmainnet2.stratisnetwork.com", "straxmainnet2.stratisnetwork.com")
            };

            this.SeedNodes = new List<NetworkAddress>
            {
            };

            this.StandardScriptsRegistry = new StraxStandardScriptsRegistry();

            Assert(this.DefaultBanTimeSeconds <= this.Consensus.MaxReorgLength * this.Consensus.TargetSpacing.TotalSeconds / 2);

            Assert(this.Consensus.HashGenesisBlock == uint256.Parse("0xebe158d09325c470276619ebc5f7f87c98c0ed4b211c46a17a6457655811d082"));
            Assert(this.Genesis.Header.HashMerkleRoot == uint256.Parse("0xdd91e99b7ca5eb97d9c41b867762d1f2db412ba4331efb61d138fce5d39b9084"));

            StraxNetwork.RegisterRules(this.Consensus);
            StraxNetwork.RegisterMempoolRules(this.Consensus);
        }
    }
}