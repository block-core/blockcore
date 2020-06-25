﻿using System;
using System.Linq;
using System.Net;
using OpenExo.Networks.Consensus;
using OpenExo.Networks.Deployments;
using OpenExo.Networks.Policies;
using OpenExo.Networks.Setup;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;

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

            Consensus = new NBitcoin.Consensus(
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

            Base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (byte)network.PubKeyAddress };
            Base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (byte)network.ScriptAddress };
            Base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (byte)network.SecretAddress };
            Base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
            Base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };

            Bech32Encoders = new Bech32Encoder[2];
            var encoder = new Bech32Encoder(network.CoinTicker.ToLowerInvariant());
            Bech32Encoders[(int)Bech32Type.WITNESS_PUBKEY_ADDRESS] = encoder;
            Bech32Encoders[(int)Bech32Type.WITNESS_SCRIPT_ADDRESS] = encoder;

            Checkpoints = network.Checkpoints;
            DNSSeeds = network.DNS.Select(dns => new DNSSeedData(dns, dns)).ToList();
            SeedNodes = network.Nodes.Select(node => new NBitcoin.Protocol.NetworkAddress(IPAddress.Parse(Dns.GetHostAddresses(node).GetValue(0).ToString()), network.DefaultPort)).ToList();

            StandardScriptsRegistry = new OpenExoStandardScriptsRegistry();

            // 64 below should be changed to TargetSpacingSeconds when we move that field.
            Assert(DefaultBanTimeSeconds <= Consensus.MaxReorgLength * 64 / 2);

            Assert(Consensus.HashGenesisBlock == uint256.Parse(network.HashGenesisBlock));
            Assert(Genesis.Header.HashMerkleRoot == uint256.Parse(network.HashMerkleRoot));

            RegisterRules(Consensus);
            RegisterMempoolRules(Consensus);
        }
    }
}
