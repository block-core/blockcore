using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.City.Networks.Setup;
using NBitcoin;

namespace Blockcore.Networks.City
{
    internal class CitySetup
    {
        internal static CitySetup Instance = new CitySetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "city",
            ConfigFileName = "city.conf",
            Magic = "01-59-54-43",
            CoinType = 1926, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 13736000000,
            PoWBlockReward = 2,
            PoSBlockReward = 20,
            LastPowBlock = 2500,
            GenesisText = "July 27, 2018, New Scientiest, Bitcoin’s roots are in anarcho-capitalism", // The New York Times, 2020-04-16
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
            PoSVersion = 3
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "CityMain",
            RootFolderName = "city",
            CoinTicker = "CITY",
            DefaultPort = 4333,
            DefaultRPCPort = 4334,
            DefaultAPIPort = 4335,
            DefaultSignalRPort = 4336,
            PubKeyAddress = 28, // B https://en.bitcoin.it/wiki/List_of_address_prefixes
            ScriptAddress = 88, // b
            SecretAddress = 237,
            GenesisTime = 1538481600,
            GenesisNonce = 1626464,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00000b0517068e602ed5279c20168cfa1e69884ee4e784909652da34c361bff2",
            HashMerkleRoot = "b3425d46594a954b141898c7eebe369c6e6a35d2dab393c1f495504d2147883b",
            DNS = new[] { "seed.city-chain.org", "seed.citychain.foundation", "seed.city-coin.org", "seed.liberstad.com", "city.seed.blockcore.net" },
            Nodes = new[] { "23.97.234.230", "13.73.143.193", "89.10.227.34" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
         {
                { 0, new CheckpointInfo(new uint256("0x00000b0517068e602ed5279c20168cfa1e69884ee4e784909652da34c361bff2"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                { 2, new CheckpointInfo(new uint256("0x072227af2fda8ef6a5f7a19ec3a1c6de54ddc537dd407da938766ed460e77982"), new uint256("0xe93eb6c21c65024ca06ac2f89481bdc832cab1607ed2adfeafb6c679b6a4a1f6")) },
                { 50, new CheckpointInfo(new uint256("0xce58ab37dd5965c3474c5917fcbb59aa342c6754a452e5faf87050bb6015d511"), new uint256("0xb877b17b3d7324ac1a3615a6c245c702282e5be74fd50cf25bb02bc5f2ea7944")) },
                { 100, new CheckpointInfo(new uint256("0x5edbf09aadfbdb0d74d428b002fcda197debb775955a161f2890ed844a5159da"), new uint256("0x354210eecb7ed3f8df3d384b8d615f789fdffdf3f3d4945c23e5966827010b73")) },
                { 150000, new CheckpointInfo(new uint256("0x0be1d4fce6a93989025d405292d12aca12c7417494e50c2c633ad2f7bb7cbb53"), new uint256("0xcaafe0d5594c6b12bd0b819ccc22dba5ae7dcea32721cd97df369dbe868e13e9")) },
                { 800000, new CheckpointInfo(new uint256("0xaf94ebd59507829e82d2e98e75f8777224bf54e2f4ad76ff7bdc2ebebc634cb9"), new uint256("0xbe19a177b90653ee3a654e7fd307e93410db3478dbc28225e24aea9d2087d04b")) },
                { 1060000, new CheckpointInfo(new uint256("0xea17e88ff533ca71dbaf0a8772d1f680845371e774250e35251671227fdcb699"), new uint256("0xfc4d6eec52900c6623711427d11357afe6becc6aafa71ed3ade3fe3128d9f23f")) },
         }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "CityRegTest",
            RootFolderName = "cityregtest",
            CoinTicker = "TCITY",
            DefaultPort = 14333,
            DefaultRPCPort = 14334,
            DefaultAPIPort = 14335,
            DefaultSignalRPort = 14336,
            PubKeyAddress = 66,
            ScriptAddress = 196,
            SecretAddress = 194,
            GenesisTime = 1587115302,
            GenesisNonce = 5917,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "000039df5f7c79084bf96c67ea24761e177d77c24f326eb5294860144301cb68",
            HashMerkleRoot = "d382311c9e4a1ec84be1b32eddb33f7f0420544a460754f573d7cb7054566d75",
            DNS = new[] { "seedregtest1.city.blockcore.net", "seedregtest2.city.blockcore.net", "seedregtest.city.blockcore.net" },
            Nodes = new[] { "23.97.234.230", "13.73.143.193", "89.10.227.34" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "CityTest",
            RootFolderName = "citytest",
            CoinTicker = "TCITY",
            DefaultPort = 24333,
            DefaultRPCPort = 24334,
            DefaultAPIPort = 24335,
            DefaultSignalRPort = 24336,
            PubKeyAddress = 66,
            ScriptAddress = 196,
            SecretAddress = 194,
            GenesisTime = 1587115303,
            GenesisNonce = 3451,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "00090058f8a37e4190aa341ab9605d74b282f0c80983a676ac44b0689be0fae1",
            HashMerkleRoot = "88cd7db112380c4d6d4609372b04cdd56c4f82979b7c3bf8c8a764f19859961f",
            DNS = new[] { "seedtest1.city.blockcore.net", "seedtest2.city.blockcore.net", "seedtest.city.blockcore.net" },
            Nodes = new[] { "23.97.234.230", "13.73.143.193", "89.10.227.34" },
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
