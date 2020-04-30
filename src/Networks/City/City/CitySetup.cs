using System;
using System.Collections.Generic;
using City.Networks;
using City.Networks.Setup;
using NBitcoin;

namespace City
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
            // TODO: Add checkpoints as the network progresses.
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
         PubKeyAddress = 111,
         ScriptAddress = 196,
         SecretAddress = 239,
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
         PubKeyAddress = 111,
         ScriptAddress = 196,
         SecretAddress = 239,
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
