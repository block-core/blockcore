using System;
using System.Collections.Generic;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.XRC.Consensus;
using Blockcore.Networks.XRC.Deployments;
using Blockcore.Networks.XRC.Policies;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace Blockcore.Networks.XRC
{
    public class XRCMain : XRCNetwork
    {
        public XRCMain()
        {
            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x33;
            messageStart[1] = 0x33;
            messageStart[2] = 0x34;
            messageStart[3] = 0x35;
            uint magic = BitConverter.ToUInt32(messageStart, 0);

            this.Name = "xRhodiumMain";
            this.NetworkType = NetworkType.Mainnet;
            this.Magic = magic;
            this.DefaultPort = 37270;
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.DefaultRPCPort = 19660;
            this.DefaultAPIPort = 37221;
            this.MaxTipAge = 604800;
            this.MinTxFee = 1000;
            this.MaxTxFee = Money.Coins(1).Satoshi;
            this.FallbackFee = 20000;
            this.MinRelayTxFee = 1000;
            this.RootFolderName = "xrhodium";
            this.DefaultConfigFilename = "xrhodium.conf";
            this.MaxTimeOffsetSeconds = 25 * 60;
            this.CoinTicker = "XRC";
            this.DefaultBanTimeSeconds = 16000; // 500 (MaxReorg) * 64 (TargetSpacing) / 2 = 4 hours, 26 minutes and 40 seconds

            var consensusFactory = new XRCConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1512043200;
            this.GenesisNonce = 0;
            this.GenesisBits = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")).ToCompact();
            this.GenesisVersion = 45;
            this.GenesisReward = Money.Zero;
            var pubKeyMain = "04ffff0f1e01041a52656c6561736520746865204b72616b656e212121205a657573";

            var consensusOptions = new PosConsensusOptions
            {
                MaxBlockBaseSize = 4 * 1000 * 1000,
                MaxBlockSerializedSize = 4 * 1000 * 1000,
                MaxStandardVersion = 2,
                MaxStandardTxWeight = (4 * 1000 * 1000) / 10,
                MaxBlockSigopsCost = 160000,
                MaxStandardTxSigopsCost = 160000 / 5, 
                WitnessScaleFactor = 1,
            };

            var buriedDeployments = new BuriedDeploymentsArray
            {
                [BuriedDeployments.BIP34] = 0,
                [BuriedDeployments.BIP65] = 0,
                [BuriedDeployments.BIP66] = 0
            };

            consensusFactory.Protocol = new XRCConsensusProtocol()
            {
                ProtocolVersion = ProtocolVersion.FEEFILTER_VERSION,
                MinProtocolVersion = ProtocolVersion.POS_PROTOCOL_VERSION,
                PowLimit2Time = 1541879606,
                PowLimit2Height = 1648,
                PowDigiShieldX11Height = 10000000,
                PowDigiShieldX11Time = 2541879606
            };

            Block genesisBlock = CreateXRCGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, pubKeyMain);
            this.Genesis = genesisBlock;

            this.Consensus = new XRCConsensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: 10291, 
                hashGenesisBlock: genesisBlock.GetHash(), 
                subsidyHalvingInterval: 210000, 
                majorityEnforceBlockUpgrade: 750, 
                majorityRejectBlockOutdated: 950, 
                majorityWindow: 1000, 
                buriedDeployments: buriedDeployments,
                bip9Deployments: new XRCBIP9Deployments(),
                bip34Hash: new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),  
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing  
                maxReorgLength: 0, 
                defaultAssumeValid: null, // 1600000 
                maxMoney: 2100000 * Money.COIN, 
                coinbaseMaturity: 10, 
                premineHeight: 1, 
                premineReward: new Money(1050000 * Money.COIN), 
                proofOfWorkReward: Money.Coins((decimal)2.5), 
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                targetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: true,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("0000000000092489000000000000000000000000000000000000000000000000")),
                powLimit2: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                minimumChainWork: uint256.Zero,
                isProofOfStake: false,
                lastPowBlock: default(int),
                proofOfStakeLimit: null,
                proofOfStakeLimitV2: null,
                proofOfStakeReward: Money.Zero,
                proofOfStakeTimestampMask: 0
            );

            this.Consensus.PosEmptyCoinbase = false;
            this.Consensus.PosUseTimeFieldInKernalHash = true;

            this.Base58Prefixes = new byte[12][];
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (61) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (123) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (100) };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 17,new CheckpointInfo(new uint256("2430c4151e10cdc5ccbdea56b909c7c37ab2a852d3e7fb908e0a32493e2ac706")) },
                { 117, new CheckpointInfo(new uint256("bf3082be3b2da88187ebeb902548b41dbff3bcac6687352e0c47d902acd28e62"))},
                { 400, new CheckpointInfo(new uint256("20cb04127f12c1ae7a04ee6dc4c7e36f4c85ee2038c92126b3fd537110d96595"))},
                { 800, new CheckpointInfo(new uint256("df37ca401ecccfc6dedf68ab76a7161496ad93d47c2a474075efb3220e3f3526"))},
                { 26800, new CheckpointInfo(new uint256("c4efd4b6fa294fd72ab6f614dd6705eea43d0a83cd03d597c3214eaaf857a4b6"))},
                { 43034, new CheckpointInfo(new uint256("4df06bd483d2c4ccde5cd1efe3b2ea7d969c41e5923a74c2bba1656a41fc6891"))},
                { 110000, new CheckpointInfo(new uint256("d1d1282681f20223a281393528e6c624539e60177ecb42ab4512555974ac7775"))},
            };

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder("rh");
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("dns.btrmine.com", "dns.btrmine.com"),
                new DNSSeedData("dns2.btrmine.com", "dns2.btrmine.com"),
                new DNSSeedData("xrc.dnsseed.ekcdd.com", "xrc.dnsseed.ekcdd.com")
            };

            this.SeedNodes = new List<NetworkAddress>(); 
            this.StandardScriptsRegistry = new XRCStandardScriptsRegistry();

            this.RegisterRules(this.Consensus);
            this.RegisterMempoolRules(this.Consensus);
        }
    }
}