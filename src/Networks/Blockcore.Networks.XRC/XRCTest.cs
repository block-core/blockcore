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
    public class XRCTest : XRCMain
    {
        public XRCTest()
        {
            // The message start string is designed to be unlikely to occur in normal data.
            // The characters are rarely used upper ASCII, not valid as UTF-8, and produce
            // a large 4-byte int at any alignment.
            var messageStart = new byte[4];
            messageStart[0] = 0x39;
            messageStart[1] = 0x33;
            messageStart[2] = 0x34;
            messageStart[3] = 0x35;
            uint magic = BitConverter.ToUInt32(messageStart, 0);

            this.Name = "xRhodiumTest";
            this.NetworkType = NetworkType.Testnet;
            this.Magic = magic;
            this.DefaultPort = 16665;
            this.DefaultMaxOutboundConnections = 16;
            this.DefaultMaxInboundConnections = 109;
            this.DefaultRPCPort = 16661;
            this.DefaultAPIPort = 16669;
            this.MaxTipAge = xRhodiumDefaultMaxTipAgeInSeconds;
            this.MinTxFee = 10000;
            this.MaxTxFee = Money.Coins(1).Satoshi;
            this.FallbackFee = 60000;
            this.MinRelayTxFee = 10000;
            this.RootFolderName = xRhodiumRootFolderName;
            this.DefaultConfigFilename = xRhodiumDefaultConfigFilename;
            this.MaxTimeOffsetSeconds = xRhodiumMaxTimeOffsetSeconds;
            this.CoinTicker = "XRC";
            this.DefaultBanTimeSeconds = 16000; // 500 (MaxReorg) * 64 (TargetSpacing) / 2 = 4 hours, 26 minutes and 40 seconds

            var consensusFactory = new XRCConsensusFactory();

            // Create the genesis block.
            this.GenesisTime = 1527811200;
            this.GenesisNonce = 0;
            this.GenesisBits = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")).ToCompact();
            this.GenesisVersion = 45;
            this.GenesisReward = Money.Zero;
            
            var pubKeyTest = "04ffff0f1e01041a52656c6561736520746865204b72616b656e212121205a657573";
            Block genesisBlock = CreateXRCGenesisBlock(consensusFactory, this.GenesisTime, this.GenesisNonce, this.GenesisBits, this.GenesisVersion, pubKeyTest);

            this.Genesis = genesisBlock;

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

            consensusFactory.Protocol = new ConsensusProtocol()
            {
                ProtocolVersion = ProtocolVersion.FEEFILTER_VERSION,
                MinProtocolVersion = ProtocolVersion.POS_PROTOCOL_VERSION,
            };

            this.Consensus = new XRCConsensus(
                consensusFactory: consensusFactory,
                consensusOptions: consensusOptions,
                coinType: 1,
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
                coinbaseMaturity: 1,
                premineHeight: 1,
                premineReward: new Money(1050000 * Money.COIN),
                proofOfWorkReward: Money.Coins((decimal)2.5),
                targetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                targetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: false,
                posNoRetargeting: true,
                powNoRetargeting: false,
                powLimit: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                powLimit2: new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                powLimit2Time: 0,
                powLimit2Height: 0,
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
            this.Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (65) };
            this.Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (128) };
            this.Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (0xef) };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            this.Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            this.Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            this.Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            this.Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            this.Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            this.Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };

            this.Checkpoints = new Dictionary<int, CheckpointInfo>();

            this.Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder("th");
            this.Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            this.Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            this.DNSSeeds = new List<DNSSeedData>();
            this.SeedNodes = new List<NetworkAddress>();
            this.StandardScriptsRegistry = new XRCStandardScriptsRegistry();

            base.RegisterRules(this.Consensus);
            base.RegisterMempoolRules(this.Consensus);
        }
    }
}
