using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.RoyalSportsCity.Networks.Setup;
using NBitcoin;

namespace Blockcore.Networks.RoyalSportsCity
{
    internal class RoyalSportsCitySetup
    {
        internal static RoyalSportsCitySetup Instance = new RoyalSportsCitySetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "royalsportscity",
            ConfigFileName = "royalsportscity.conf",
            Magic = "01-52-53-43",
            CoinType = 6599,
            PremineReward = 3000000000,
            PoWBlockReward = 210,
            PoSBlockReward = 21,
            LastPowBlock = 2100,
            MaxSupply = 21000000000,
            GenesisText = "Decentralized Royal  Sports City",
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
            PoSVersion = 4
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "RoyalSportsCityMain",
            RootFolderName = "royalsportscity",
            CoinTicker = "RSC",
            DefaultPort = 14001,
            DefaultRPCPort = 14002,
            DefaultAPIPort = 14003,
            PubKeyAddress = 60, // R 
            ScriptAddress = 122, // r
            SecretAddress = 122,
            GenesisTime = 1641376672,
            GenesisNonce = 1019548,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "000009b276ed4815cf1fde9078f2d21facd5ece23e6ccfb31c7f90f87563010f",
            HashMerkleRoot = "d8575410f6b019171ff0b4548814e7ae6fc3dd837d5ecc71d77bb21866ddb210",
            DNS = new[] { "seed.royalsportscity.com", "seed.royalsportscity.net" },
            Nodes = new[] { "188.40.181.18", "46.105.172.12", "51.89.132.133", "152.228.148.188" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "RoyalSportsCityRegTest",
            RootFolderName = "royalsportscityregtest",
            CoinTicker = "TRSC",
            DefaultPort = 24001,
            DefaultRPCPort = 24002,
            DefaultAPIPort = 24003,
            PubKeyAddress = 60, // R 
            ScriptAddress = 122, // r
            SecretAddress = 122,
            GenesisTime = 1641376738,
            GenesisNonce = 128636,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00007a766fd57aef404cc1ae16d74f616074c924d41aeed23e727f9a00b83c01",
            HashMerkleRoot = "f547b27d5390b11f578ff3b0855b9b21547bbca3b54f39820c2de371001ea71f",
            DNS = new[] { "seedregtest.royalsportscity.com", "seedregtest.royalsportscity.net" },
            Nodes = new[] { "188.40.181.18", "46.105.172.12", "51.89.132.133", "152.228.148.188" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "RoyalSportsCityTest",
            RootFolderName = "royalsportscitytest",
            CoinTicker = "TRSC",
            DefaultPort = 34001,
            DefaultRPCPort = 34002,
            DefaultAPIPort = 34003,
            PubKeyAddress = 60, // R 
            ScriptAddress = 122, // r
            SecretAddress = 122,
            GenesisTime = 1641376746,
            GenesisNonce = 7719,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0004e385d4eb63c3a8f2a190117330a3dbef67782b33a5a29e1b554ed357d5df",
            HashMerkleRoot = "92711a71fa912dd3419c5e52bee7edc64ce1d60260ba76664f49c39367734d83",
            DNS = new[] { "seedtest.royalsportscity.com", "seedtest.royalsportscity.net" },
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
