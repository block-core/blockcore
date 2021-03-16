using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using HomeCoin.Networks.Setup;
using NBitcoin;

namespace HomeCoin
{
    internal class HomeCoinSetup
    {
        internal static HomeCoinSetup Instance = new HomeCoinSetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "homecoin",
            ConfigFileName = "homecoin.conf",
            Magic = "48-4F-4D-45",
            CoinType = 100500, //0x80018894// SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 1000000,
            PoWBlockReward = 10,
            PoSBlockReward = 10,
            LastPowBlock = 100500,
            MaxSupply = 21000000,
            GenesisText = "I'd like to know what this whole show is all about before it's out.",
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F,
            PoSVersion = 3
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "HomeCoinMain",
            RootFolderName = "homecoin",
            CoinTicker = "HOME",
            DefaultPort = 33331,
            DefaultRPCPort = 33332,
            DefaultAPIPort = 33333,
            DefaultSignalRPort = 33334,
            PubKeyAddress = 40, // H https://en.bitcoin.it/wiki/List_of_address_prefixes
            ScriptAddress = 100, // h
            SecretAddress = 160,
            GenesisTime = 1614259698,
            GenesisNonce = 60494,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00000ae66e376bf115b9440f03a520bd88d28624ec6f13606b0d72051e56e635",
            HashMerkleRoot = "f54bfdb51ebda155c70525ed6ef4fee32ca2564b8532767661acf42015cd5542",
            DNS = new[] { "seed.homecoin.ru", "seed2.homecoin.ru", "seed3.homecoin.ru", "seed4.homecoin.ru", "seed5.homecoin.ru" },
            Nodes = new[] { "167.86.77.3", "167.86.126.130", "158.101.197.109", "158.101.206.15", "40.76.201.247" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x00000ae66e376bf115b9440f03a520bd88d28624ec6f13606b0d72051e56e635"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) }, // Genisis
                { 2, new CheckpointInfo(new uint256("0x346e8a928dc03b3d92249cc67666d905a92b09f18b7bb5308198d7a28562bd98"), new uint256("0x8503c0bca8aeca76ed38648896d164637bf7229f5d9865d2b26448d448675c43")) }, // Premine
                { 1000, new CheckpointInfo(new uint256("0x10b7137bc293d3c9426b29548ff902fca123f60e7af8a844c8fa151a736dbb37"), new uint256("0x228f509a5761ca04c14e2c3aa1b2a3249485525dbc9c33e19271e13404c0a7ee")) },
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "HomeCoinRegTest",
            RootFolderName = "homecoinregtest",
            CoinTicker = "THOME",
            DefaultPort = 43331,
            DefaultRPCPort = 43332,
            DefaultAPIPort = 43333,
            DefaultSignalRPort = 43334,
            PubKeyAddress = 111,
            ScriptAddress = 196,
            SecretAddress = 239,
            GenesisTime = 1614259704,
            GenesisNonce = 83376,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00008e71ca88630fe4f8156b826f04f564858315e0d2d38ffaf583f3c269de44",
            HashMerkleRoot = "6c68247b2a96dabbcd8bffd2b0cc9adbca29662c7a94acedf3543ac3a98e9806",
            DNS = new[] { "regtestseed.homecoin.ru", "regtestseed2.homecoin.ru", "regtestseed3.homecoin.ru", "regtestseed4.homecoin.ru", "regtestseed5.homecoin.ru" },
            Nodes = new[] { "167.86.77.3", "167.86.126.130", "158.101.197.109", "158.101.196.76", "40.76.201.247" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "HomeCoinTest",
            RootFolderName = "homecointest",
            CoinTicker = "THOME",
            DefaultPort = 53331,
            DefaultRPCPort = 53331,
            DefaultAPIPort = 53331,
            DefaultSignalRPort = 53334,
            PubKeyAddress = 111,
            ScriptAddress = 196,
            SecretAddress = 239,
            GenesisTime = 1614259712,
            GenesisNonce = 1845,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0008c7ff991b012b8dc06bd91165b64b307e53f183d070850d8381f238c672df",
            HashMerkleRoot = "277acb69460036d6f4aa04408164be0c38d6cc1c10cc771fb1385696036fe742",
            DNS = new[] { "testseed.homecoin.ru", "testseed2.homecoin.ru", "testseed3.homecoin.ru", "testseed4.homecoin.ru", "testseed5.homecoin.ru" },
            Nodes = new[] { "167.86.77.3", "167.86.126.130", "158.101.197.109", "158.101.196.76", "40.76.201.247" },
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
