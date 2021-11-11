using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using NBitcoin;


namespace Blockcore.Networks.Cybits.Setup
{
    internal class CybitsSetup
    {
        internal static CybitsSetup Instance = new CybitsSetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "cybits",
            ConfigFileName = "cybits.conf",
            Magic = "01-4D-59-43",
            CoinType = 3601, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 1000000000,
            PoWBlockReward = 1500,
            PoSBlockReward = 1500,
            LastPowBlock = 150,
            GenesisText = "1st November 2021 : ekathimerini.com - Inflation eats into savings. Increase in producer...", // The New York Times, 2020-04-16
            TargetSpacing = TimeSpan.FromSeconds(48),
            ProofOfStakeTimestampMask = 0x15, // 0x0000003F // 64 sec
            PoSVersion = 4
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "CybitsMain",
            RootFolderName = "cybits",
            CoinTicker = "CY",
            DefaultPort = 17771,
            DefaultRPCPort = 17772,
            DefaultAPIPort = 17773,
            PubKeyAddress = 28, // C https://en.bitcoin.it/wiki/List_of_address_prefixes
            ScriptAddress = 87, // c
            SecretAddress = 160,
            GenesisTime = 1635788455,
            GenesisNonce = 685422,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0000076eed892cefa895d3ae78ff8f8a177d5c76e5f0ffda0cdad1533ae0d2d0",
            HashMerkleRoot = "5cb9941ec7c082628232647e5fc349996db19300e684e54a9f66fb5bc2b13261",
            DNS = new[] { "seed1.cybits.org", "seed2.cybits.org", "seed3.cybits.org" },
            Nodes = new[] { "144.91.123.46", "161.97.135.78", "144.91.95.234", "161.97.86.48" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "CybitsRegTest",
            RootFolderName = "cybitsregtest",
            CoinTicker = "TCY",
            DefaultPort = 25001,
            DefaultRPCPort = 25002,
            DefaultAPIPort = 25003,
            PubKeyAddress = 111,
            ScriptAddress = 196,
            SecretAddress = 239,
            GenesisTime = 1635788507,
            GenesisNonce = 182442,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00006bef8f058d0964b8d7f8521c2ffd3994d546251bbdcd39d93c1e67109711",
            HashMerkleRoot = "44c25c3b230896ed89c6a9d25cfbea4d6220cd920c4d9c3e2329aa28087905cf",
            DNS = new[] { "seed1.cybits.org", "seed2.cybits.org", "seed3.cybits.org" },
            Nodes = new[] { "144.91.123.46", "161.97.135.78", "144.91.95.234", "161.97.86.48" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "CybitsTest",
            RootFolderName = "cybitstest",
            CoinTicker = "TCY",
            DefaultPort = 35001,
            DefaultRPCPort = 35002,
            DefaultAPIPort = 35003,
            PubKeyAddress = 111,
            ScriptAddress = 196,
            SecretAddress = 239,
            GenesisTime = 1635788524,
            GenesisNonce = 6273,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00031baf22e64c112134554c649ee97ad76f0d93d476a4b39b19d2e6afc23b43",
            HashMerkleRoot = "7d4a15b55ff30d379c8250e695036f80a6911b3e406fa59ba12216adb564a560",
            DNS = new[] { "seed1.cybits.org", "seed2.cybits.org", "seed3.cybits.org" },
            Nodes = new[] { "144.91.123.46", "161.97.135.78", "144.91.95.234", "161.97.86.48" },
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
