using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NBitcoin;
using NBitcoin.Protocol;

namespace Impleum
{
   public class ImpleumSetup
   {
      public const string FileNamePrefix = "impleum";
      public const string ConfigFileName = "impleum.conf";
      public const string Magic = "51-11-41-31";
      public const int CoinType = 769; // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md
      public const decimal PremineReward = 1000000;
      public const decimal PoWBlockReward = 48;
      public const decimal PoSBlockReward = 5;
      public const int LastPowBlock = 100000;
      public const string GenesisText = "https://cryptocrimson.com/news/apple-payment-request-api-ripple-interledger-protocol"; // The New York Times, 2020-04-16
      public static TimeSpan TargetSpacing = TimeSpan.FromSeconds(64);
      public const uint ProofOfStakeTimestampMask = 0x0000000F; // 0x0000003F // 64 sec
      public const int PoSVersion = 3;

      public class Main
      {
         public const string Name = "ImpleumMain";
         public const string RootFolderName = "impleum";
         public const string CoinTicker = "IMPL";
         public const int DefaultPort = 16171;
         public const int DefaultRPCPort = 16172;
         public const int DefaultAPIPort = 16173;
         public const int DefaultSignalRPort = 16174;
         public const int PubKeyAddress = 102; // B https://en.bitcoin.it/wiki/List_of_address_prefixes
         public const int ScriptAddress = 125; // b
         public const int SecretAddress = 191;

         public const uint GenesisTime = 1523364655;
         public const uint GenesisNonce = 2380297;
         public const uint GenesisBits = 0x1e0fffff;
         public const int GenesisVersion = 1;
         public static Money GenesisReward = Money.Zero;
         public const string HashGenesisBlock = "0x02a8be139ec629b13df22e7abc7f9ad5239df39efaf2f5bf3ab5e4d102425dbe";
         public const string HashMerkleRoot = "0xbd3233dd8d4e7ce3ee8097f4002b4f9303000a5109e02a402d41d2faf74eb244";

         public static List<DNSSeedData> DNS = new List<DNSSeedData>
         {
            // TODO: Add additional DNS seeds here
            new DNSSeedData("impleum.com", "impleum.com"),
            new DNSSeedData("explorer.impleum.com", "explorer.impleum.com"),
            new DNSSeedData("seed.impl.blockcore.net", "seed.impl.blockcore.net"),
         };

         public static List<NetworkAddress> Nodes = new List<NetworkAddress>
         {
            // TODO: Add additional seed nodes here
            new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
            new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort),
         };

         public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         };
      }

      public class RegTest
      {
         public const string Name = "ImpleumRegTest";
         public const string RootFolderName = "ImpleumRegTest";
         public const string CoinTicker = "TIMPL";
         public const int DefaultPort = 26171;
         public const int DefaultRPCPort = 26172;
         public const int DefaultAPIPort = 26173;
         public const int DefaultSignalRPort = 26174;
         public const int PubKeyAddress = 111;
         public const int ScriptAddress = 196;
         public const int SecretAddress = 239;

         public const uint GenesisTime = 1523364655;
         public const uint GenesisNonce = 2380297;
         public const uint GenesisBits = 0x1e0fffff;
         public const int GenesisVersion = 1;
         public static Money GenesisReward = Money.Zero;
         public const string HashGenesisBlock = "0x02a8be139ec629b13df22e7abc7f9ad5239df39efaf2f5bf3ab5e4d102425dbe";
         public const string HashMerkleRoot = "0xbd3233dd8d4e7ce3ee8097f4002b4f9303000a5109e02a402d41d2faf74eb244";

         public static List<DNSSeedData> DNS = new List<DNSSeedData>
         {
            // TODO: Add additional DNS seeds here
            new DNSSeedData("seedregtest1.impl.blockcore.net", "seedregtest1.impl.blockcore.net"),
            new DNSSeedData("seedregtest2.impl.blockcore.net", "seedregtest2.impl.blockcore.net"),
            new DNSSeedData("seedregtest.impl.blockcore.net", "seedregtest.impl.blockcore.net"),
         };

         public static List<NetworkAddress> Nodes = new List<NetworkAddress>
         {
            // TODO: Add additional seed nodes here
            new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
            new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort),
         };

         public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         };
      }

      public class Test
      {
         public const string Name = "ImpleumTest";
         public const string RootFolderName = "ImpleumTest";
         public const string CoinTicker = "TIMPL";
         public const int DefaultPort = 36171;
         public const int DefaultRPCPort = 36172;
         public const int DefaultAPIPort = 36173;
         public const int DefaultSignalRPort = 36174;
         public const int PubKeyAddress = 111;
         public const int ScriptAddress = 196;
         public const int SecretAddress = 239;

         public const uint GenesisTime = 1523364655;
         public const uint GenesisNonce = 2380297;
         public const uint GenesisBits = 0x1e0fffff;
         public const int GenesisVersion = 1;
         public static Money GenesisReward = Money.Zero;
         public const string HashGenesisBlock = "0x02a8be139ec629b13df22e7abc7f9ad5239df39efaf2f5bf3ab5e4d102425dbe";
         public const string HashMerkleRoot = "0xbd3233dd8d4e7ce3ee8097f4002b4f9303000a5109e02a402d41d2faf74eb244";

         public static List<DNSSeedData> DNS = new List<DNSSeedData>
         {
            // TODO: Add additional DNS seeds here
            new DNSSeedData("seedtest1.impl.blockcore.net", "seedtest1.impl.blockcore.net"),
            new DNSSeedData("seedtest2.impl.blockcore.net", "seedtest2.impl.blockcore.net"),
            new DNSSeedData("seedtest.impl.blockcore.net", "seedtest.impl.blockcore.net"),
         };

         public static List<NetworkAddress> Nodes = new List<NetworkAddress>
         {
            // TODO: Add additional seed nodes here
            new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
            new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort),
         };

         public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
         {
                { 0, new CheckpointInfo(new uint256("0x02a8be139ec629b13df22e7abc7f9ad5239df39efaf2f5bf3ab5e4d102425dbe"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                { 2, new CheckpointInfo(new uint256("0x49c9ce21a7916a4026e782a4728ba626b02cc6caf35615c4d7c9400ad22b5282"), new uint256("0xeda93767b1d501313d9dd2cc77e1dbb256101b351eb17e3a4ab9663d3f0a3cd3")) }, // Premine
                { 35, new CheckpointInfo(new uint256("0x477d36da0993b3a5e279fd0eba7ab4825b4ff54f0c3e55b8df4e7e6c1afe6939"), new uint256("0xa5bef352cb2182f7ca80f5d6d7a4e6ce4325bcd78bab63979d4ec8871e95a53d")) },
                { 2500, new CheckpointInfo(new uint256("0x49a2d1719097b5d9ec81d89627eaa71dfefb158cb0bc0ac58051d5ca0089dd98"), new uint256("0xf6494f64e49e8e9f6092686c78af20b7eb868bee6f0ae6a97da40b4dc06e84a7")) },
                { 4000, new CheckpointInfo(new uint256("0xbd4c0a8c11431012f1b59be225b5913a1f06e1225e85a10216f2be5db1b4c0f1"), new uint256("0x79dca584714897d88de42e9540e1bdabe8df0e5fa17473014c529385b64f7c1e")) },
                { 34000, new CheckpointInfo(new uint256("0xe490b0d5eda1874bdb3ce5e2567e3b51b26bd73c05d9e9c83f614d634093a8a8"), new uint256("0x6d55c9cb5a782bc230c082adcecb9ac40357e473e550dd2fe9a6f09a0720f581")) },
                { 40000, new CheckpointInfo(new uint256("0x95c59e88378c0a1fed38f0797a49d3e7bb63ab9079b56a86c8eedaa570cfd672"), new uint256("0x28b6c48c1358c14a629041e2a019146230b6eefc7bc4cc3aa59ebcf16a78f072")) },
                { 71000, new CheckpointInfo(new uint256("0x298ee8172927a727ba26820d53ba491fc2025f275bd25513203e643593849501"), new uint256("0x06481d91ef914c1516878023bed382d1fa566d41c1c5b654f8f558b39ce0da24")) },
                { 100000, new CheckpointInfo(new uint256("0x54cdf03ca463e416bc1c759bf9e6e5367f06288c47805b421ff33f731a9ffcd3"), new uint256("0x170515e55951cd8441d0760c7daec334ed4baf975157f68486165e3d8033ffc6")) },
                { 150000, new CheckpointInfo(new uint256("0x8c8b2a0746e2e8f277807d5af79926f129fd1ea15397d27359aaa9e9eee104e9"), new uint256("0x5613eb679afae1c12012e4e2126c0195b36da333b942a3a9dc40ac8744aa10b1")) },
                { 200000, new CheckpointInfo(new uint256("0xa8c3901f1752cea4defdea41ee94221eb9b43c8836b995eb1bb873538b7d18b4"), new uint256("0x68b02912f24bc15e776c2f9655c012d177f2473bcc51b1a1842d56e617e90a3c")) },
                { 215001, new CheckpointInfo(new uint256("0x04c2b9fe7e52e0c6d54fbdf5018fcda8709457b1c12b0dc8eae185b2018de19a"), new uint256("0xe24b26116717993c4a97f4c4f4487695eba8f53fab5f8c023f1fb6c2d3d4c179")) },
                { 250000, new CheckpointInfo(new uint256("0x36bca99e22d680d6bb4b2dd7b844e1a939925d5bebe320f1d9f5c7c8adb87882"), new uint256("0xc099f94116a37d314688a75afc6a3a15cc7b04e0fec0383db28647243200c5e5")) },
                { 475000, new CheckpointInfo(new uint256("0x5438e23dda186146b0e58de04206ff455392501474d3907c615a6e55116b02fd"), new uint256("0x615285eae6d269e32898fdcac811278ddefb4bde759fa061cde4aa2053cd6585")) }
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
