using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NBitcoin;
using NBitcoin.Protocol;

namespace City
{
   public class CitySetup
   {
      public const string FileNamePrefix = "city";
      public const string ConfigFileName = "city.conf";
      public const string Magic = "01-59-54-43";
      public const int CoinType = 1926; // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md
      public const decimal PremineReward = 13736000000;
      public const decimal PoWBlockReward = 2;
      public const decimal PoSBlockReward = 20;
      public const int LastPowBlock = 2500;
      public const string GenesisText = "July 27, 2018, New Scientiest, Bitcoin’s roots are in anarcho-capitalism"; // The New York Times, 2020-04-16
      public static TimeSpan TargetSpacing = TimeSpan.FromSeconds(64);
      public const uint ProofOfStakeTimestampMask = 0x0000000F; // 0x0000003F // 64 sec
      public const int PoSVersion = 3;

      public class Main
      {
         public const string Name = "CityMain";
         public const string RootFolderName = "city";
         public const string CoinTicker = "CITY";
         public const int DefaultPort = 4333;
         public const int DefaultRPCPort = 4334;
         public const int DefaultAPIPort = 4335;
         public const int DefaultSignalRPort = 4336;
         public const int PubKeyAddress = 28; // B https://en.bitcoin.it/wiki/List_of_address_prefixes
         public const int ScriptAddress = 88; // b
         public const int SecretAddress = 237;

         public const uint GenesisTime = 1538481600;
         public const uint GenesisNonce = 1626464;
         public const uint GenesisBits = 0x1E0FFFFF;
         public const int GenesisVersion = 1;
         public static Money GenesisReward = Money.Zero;
         public const string HashGenesisBlock = "00000b0517068e602ed5279c20168cfa1e69884ee4e784909652da34c361bff2";
         public const string HashMerkleRoot = "b3425d46594a954b141898c7eebe369c6e6a35d2dab393c1f495504d2147883b";

         public static List<DNSSeedData> DNS = new List<DNSSeedData>
         {
            // TODO: Add additional DNS seeds here
            new DNSSeedData("seed.city-chain.org", "seed.city-chain.org"),
            new DNSSeedData("seed.citychain.foundation", "seed.citychain.foundation"),
            new DNSSeedData("city.seed.blockcore.net", "city.seed.blockcore.net"),
         };

         public static List<NetworkAddress> Nodes = new List<NetworkAddress>
         {
            // TODO: Add additional seed nodes here
            new NetworkAddress(IPAddress.Parse("23.97.234.230"), CitySetup.Test.DefaultPort),
            new NetworkAddress(IPAddress.Parse("13.73.143.193"), CitySetup.Test.DefaultPort),
         };

         public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         };
      }

      public class RegTest
      {
         public const string Name = "CityRegTest";
         public const string RootFolderName = "CityRegTest";
         public const string CoinTicker = "TCITY";
         public const int DefaultPort = 14333;
         public const int DefaultRPCPort = 14334;
         public const int DefaultAPIPort = 14335;
         public const int DefaultSignalRPort = 14336;
         public const int PubKeyAddress = 111;
         public const int ScriptAddress = 196;
         public const int SecretAddress = 239;

         public const uint GenesisTime = 1587115302;
         public const uint GenesisNonce = 5917;
         public const uint GenesisBits = 0x1F00FFFF;
         public const int GenesisVersion = 1;
         public static Money GenesisReward = Money.Zero;
         public const string HashGenesisBlock = "000039df5f7c79084bf96c67ea24761e177d77c24f326eb5294860144301cb68";
         public const string HashMerkleRoot = "d382311c9e4a1ec84be1b32eddb33f7f0420544a460754f573d7cb7054566d75";

         public static List<DNSSeedData> DNS = new List<DNSSeedData>
         {
            // TODO: Add additional DNS seeds here
            new DNSSeedData("seedregtest1.city.blockcore.net", "seedregtest1.city.blockcore.net"),
            new DNSSeedData("seedregtest2.city.blockcore.net", "seedregtest2.city.blockcore.net"),
            new DNSSeedData("seedregtest.city.blockcore.net", "seedregtest.city.blockcore.net"),
         };

         public static List<NetworkAddress> Nodes = new List<NetworkAddress>
         {
            // TODO: Add additional seed nodes here
            new NetworkAddress(IPAddress.Parse("23.97.234.230"), CitySetup.Test.DefaultPort),
            new NetworkAddress(IPAddress.Parse("13.73.143.193"), CitySetup.Test.DefaultPort),
         };

         public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         };
      }

      public class Test
      {
         public const string Name = "CityTest";
         public const string RootFolderName = "CityTest";
         public const string CoinTicker = "TCITY";
         public const int DefaultPort = 24333;
         public const int DefaultRPCPort = 24334;
         public const int DefaultAPIPort = 24335;
         public const int DefaultSignalRPort = 24336;
         public const int PubKeyAddress = 111;
         public const int ScriptAddress = 196;
         public const int SecretAddress = 239;

         public const uint GenesisTime = 1587115303;
         public const uint GenesisNonce = 3451;
         public const uint GenesisBits = 0x1F0FFFFF;
         public const int GenesisVersion = 1;
         public static Money GenesisReward = Money.Zero;
         public const string HashGenesisBlock = "00090058f8a37e4190aa341ab9605d74b282f0c80983a676ac44b0689be0fae1";
         public const string HashMerkleRoot = "88cd7db112380c4d6d4609372b04cdd56c4f82979b7c3bf8c8a764f19859961f";

         public static List<DNSSeedData> DNS = new List<DNSSeedData>
         {
            // TODO: Add additional DNS seeds here
            new DNSSeedData("seedtest1.city.blockcore.net", "seedtest1.city.blockcore.net"),
            new DNSSeedData("seedtest2.city.blockcore.net", "seedtest2.city.blockcore.net"),
            new DNSSeedData("seedtest.city.blockcore.net", "seedtest.city.blockcore.net"),
         };

         public static List<NetworkAddress> Nodes = new List<NetworkAddress>
         {
            // TODO: Add additional seed nodes here
            new NetworkAddress(IPAddress.Parse("23.97.234.230"), CitySetup.Test.DefaultPort),
            new NetworkAddress(IPAddress.Parse("13.73.143.193"), CitySetup.Test.DefaultPort),
         };

         public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         };
      }

      public static bool IsPoSv3()
      {
         return PoSVersion == 3;
      }

      public static bool IsPoSv4()
      {
         return PoSVersion == 4;
      }
   }
}
