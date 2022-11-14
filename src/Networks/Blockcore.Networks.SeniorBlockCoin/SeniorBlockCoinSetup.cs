using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.SeniorBlockCoin.Networks.Setup;
using NBitcoin;

namespace Blockcore.Networks.SeniorBlockCoin
{
    internal class SeniorBlockCoinSetup
    {
        internal static SeniorBlockCoinSetup Instance = new SeniorBlockCoinSetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "seniorblockcoin",
            ConfigFileName = "seniorblockcoin.conf",
            Magic = "01-53-42-43",
            CoinType = 5006,
            PremineReward = 256000000,
            PoWBlockReward = 100,
            PoSBlockReward = 100,
            LastPowBlock = 25000,
            MaxSupply = 1024000000,
            GenesisText = "The world becomes decentralized",
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
            PoSVersion = 4
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "SeniorBlockCoinMain",
            RootFolderName = "seniorblockcoin",
            CoinTicker = "SBC",
            DefaultPort = 15006,
            DefaultRPCPort = 15007,
            DefaultAPIPort = 15008,
            PubKeyAddress = 63, // S 
            ScriptAddress = 125, // s
            SecretAddress = 125,
            GenesisTime = 1641366830,
            GenesisNonce = 886216,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00000766bf653d2d1916a934ea96b4b99082551a90dccf7658e5b7d7206eef28",
            HashMerkleRoot = "a0745296c43b4fdcc4a11f00e70fd961f8c52a70f9e8a87460f251c6400cf977",
            DNS = new[] { "seed.seniorblockchain.io", "seed.seniorblockchain.net" },
            Nodes = new[] { "188.40.181.18", "46.105.172.12", "51.89.132.133", "152.228.148.188" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "SeniorBlockCoinRegTest",
            RootFolderName = "seniorblockcoinregtest",
            CoinTicker = "TSBC",
            DefaultPort = 25006,
            DefaultRPCPort = 25007,
            DefaultAPIPort = 25008,
            PubKeyAddress = 63,
            ScriptAddress = 125,
            SecretAddress = 125,
            GenesisTime = 1641366887,
            GenesisNonce = 8148,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0000f6d99aebfa75bf6fa83d6585506d2d51ba1a7751b2883581260005310124",
            HashMerkleRoot = "a7719b19e90f6d0856f6a9dde26501c9d70c9a3c1d958c601cd13d2fc3d57a03",
            DNS = new[] { "seedregtest.seniorblockchain.io", "seedregtest.seniorblockchain.net" },
            Nodes = new[] { "188.40.181.18", "46.105.172.12", "51.89.132.133", "152.228.148.188" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "SeniorBlockCoinTest",
            RootFolderName = "seniorblockcointest",
            CoinTicker = "TSBC",
            DefaultPort = 35006,
            DefaultRPCPort = 35007,
            DefaultAPIPort = 35008,
            PubKeyAddress = 63,
            ScriptAddress = 125,
            SecretAddress = 125,
            GenesisTime = 1641366888,
            GenesisNonce = 4218,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0004a118ebb009cbb2530f6e4a1166909252c45f0f1f28a517201dcbed24c317",
            HashMerkleRoot = "08b098c826e241820adc2de28a30412cdb70b500260f6c248cc426a4706e3d80",
            DNS = new[] { "seedtest.seniorblockchain.io", "seedtest.seniorblockchain.net" },
            Nodes = new[] { "188.40.181.18", "46.105.172.12", "51.89.132.133", "152.228.148.188" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        public bool IsPoSv3()
        {
            return this.Setup.PoSVersion == 3;
        }

        public bool IsPoSv4()
        {
            return this.Setup.PoSVersion == 4;
        }
    }
}
