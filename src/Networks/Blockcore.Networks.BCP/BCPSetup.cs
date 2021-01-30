using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.BCP.Networks.Setup;
using NBitcoin;

namespace Blockcore.Networks.BCP
{
    internal class BCPSetup
    {
        internal static BCPSetup Instance = new BCPSetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "bcp",
            ConfigFileName = "bcp.conf",
            Magic = "42-43-50-30",
            CoinType = 2009, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 0,
            PoWBlockReward = 1,
            PoSBlockReward = 1,
            LastPowBlock = 12500,
            GenesisText = "build blockchains",
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
            PoSVersion = 4
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "BCPMain",
            RootFolderName = "bcp",
            CoinTicker = "BCP",
            DefaultPort = 15001,
            DefaultRPCPort = 15002,
            DefaultAPIPort = 15003,
            PubKeyAddress = 58, // B https://en.bitcoin.it/wiki/List_of_address_prefixes
            ScriptAddress = 120, // b
            SecretAddress = 125,
            GenesisTime = 1610818015,
            GenesisNonce = 1374370,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "000007841f9dc0a4e12e127a66bf895d526a5aad9f6857482f6c0edb89a09cf1",
            HashMerkleRoot = "4f931c99c7dc688b6e2284ad8d695e2cfb19af115749b8e3f485f8cdaabff1a7",
            DNS = new[] { "seed.blockcore.net", "bcp.seed.blockcore.net" },
            Nodes = new[] { "89.10.229.203" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "BCPRegTest",
            RootFolderName = "bcpregtest",
            CoinTicker = "TBCP",
            DefaultPort = 25001,
            DefaultRPCPort = 25002,
            DefaultAPIPort = 25003,
            PubKeyAddress = 111,
            ScriptAddress = 196,
            SecretAddress = 239,
            GenesisTime = 1610818118,
            GenesisNonce = 81461,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00006ea4efad31207df6a5d60ab4399b95f3eee9a5381d848e6105c60ea63766",
            HashMerkleRoot = "1eee168fcc801467f7ae5379f94400f15f771800e06451c6246b50d00143d1d8",
            DNS = new[] { "seedregtest1.bcp.blockcore.net", "seedregtest.bcp.blockcore.net" },
            Nodes = new[] { "89.10.229.203" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "BCPTest",
            RootFolderName = "bcptest",
            CoinTicker = "TBCP",
            DefaultPort = 35001,
            DefaultRPCPort = 35002,
            DefaultAPIPort = 35003,
            PubKeyAddress = 111,
            ScriptAddress = 196,
            SecretAddress = 239,
            GenesisTime = 1610818124,
            GenesisNonce = 819,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "000f2d143b4fe0fedb60b80d75fbdb2b91af97dbcf2f053d0a3c22f73557635e",
            HashMerkleRoot = "e3c2c263b5dd0b18ce12957872792615a27dd946ca7c2eb88d5ddb05ea14973d",
            DNS = new[] { "seedtest1.bcp.blockcore.net", "seedtest.bcp.blockcore.net" },
            Nodes = new[] { "89.10.229.203" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        public bool IsPoSv3()
        {
            return Setup.PoSVersion == 3;
        }

        public bool IsPoSv4()
        {
            return Setup.PoSVersion == 4;
        }
    }
}
