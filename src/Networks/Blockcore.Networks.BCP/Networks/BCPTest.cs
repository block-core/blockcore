using System;
using System.Linq;
using System.Net;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Networks;
using Blockcore.Networks.BCP.Networks.Policies;
using Blockcore.Networks.BCP.Networks.Setup;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

namespace Blockcore.Networks.BCP.Networks
{
    public class BCPTest : BCPMain
    {
        public BCPTest()
        {
            CoinSetup setup = BCPSetup.Instance.Setup;
            NetworkSetup network = BCPSetup.Instance.Test;

            NetworkType = NetworkType.Testnet;

            Name = network.Name;
            CoinTicker = network.CoinTicker;
            Magic = ConversionTools.ConvertToUInt32(setup.Magic, true);
            RootFolderName = network.RootFolderName;
            DefaultPort = network.DefaultPort;
            DefaultRPCPort = network.DefaultRPCPort;
            DefaultAPIPort = network.DefaultAPIPort;

            var consensusFactory = new PosConsensusFactory();

            // Create the genesis block.
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
                bip9Deployments: new NoBIP9Deployments(),
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

            Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
            Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
            Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
            Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
            Base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
            Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
            Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
            Base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
            Base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
            Base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };

            Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder(network.CoinTicker.ToLowerInvariant());
            Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            Checkpoints = network.Checkpoints;
            DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
            SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(node), network.DefaultPort)).ToList();

            StandardScriptsRegistry = new BCPStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(DefaultBanTimeSeconds <= Consensus.MaxReorgLength * 64 / 2);

            Assert(Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
            Assert(Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

            RegisterRules(Consensus);
            RegisterMempoolRules(Consensus);
        }
    }
}
