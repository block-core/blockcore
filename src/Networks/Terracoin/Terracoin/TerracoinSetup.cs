using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Terracoin.Networks;
using Terracoin.Networks.Setup;
using NBitcoin;

namespace Terracoin
{
   internal class TerracoinSetup
   {
      internal static TerracoinSetup Instance = new TerracoinSetup();

      internal CoinSetup Setup = new CoinSetup
      {
         FileNamePrefix = "terracoin",
         ConfigFileName = "terracoin.conf",
         Magic = "1455340098",
         CoinType = 83, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
         PremineReward = 0,
         PoWBlockReward = 20,
         PoSBlockReward = 0,
         LastPowBlock = 0,
         GenesisText = "June 4th 1978 - March 6th 2009 ; Rest In Peace, Stephanie.", // The New York Times, 2020-04-16
         TargetSpacing = TimeSpan.FromSeconds(64),
         ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
         PoSVersion = 3
      };

      internal NetworkSetup Main = new NetworkSetup
      {
         Name = "TerracoinMain",
         RootFolderName = "terracoin",
         CoinTicker = "TRC",
         DefaultPort = 13333,
         DefaultRPCPort = 13332,
         DefaultAPIPort = 13332,
         PubKeyAddress = 111, // B https://en.bitcoin.it/wiki/List_of_address_prefixes
         ScriptAddress = 196, // b
         SecretAddress = 239,
         GenesisTime = --genesis-nonce-main,
         GenesisNonce = 631024,
         GenesisBits = 0x,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "",
         HashMerkleRoot = "",
         DNS = new[] { "seed.terracoin.io", "dnsseed.southofheaven.ca", "trc.seed.blockcore.net" },
         Nodes = new[] { "104.238.156.46", "107.170.238.241" },
         Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         }
      };

      internal NetworkSetup RegTest = new NetworkSetup
      {
         Name = "TerracoinRegTest",
         RootFolderName = "terracoinregtest",
         CoinTicker = "TTRC",
         DefaultPort = 18444,
         DefaultRPCPort = 18332,
         DefaultAPIPort = 18332,
         PubKeyAddress = 111,
         ScriptAddress = 196,
         SecretAddress = 239,
         GenesisTime = --genesis-nonce-regtest,
         GenesisNonce = 41450,
         GenesisBits = 0x,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "",
         HashMerkleRoot = "",
         DNS = new[] { "seedregtest1.trc.blockcore.net", "seedregtest2.trc.blockcore.net", "seedregtest.trc.blockcore.net" },
         Nodes = new[] { "104.238.156.46", "107.170.238.241" },
         Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         }
      };

      internal NetworkSetup Test = new NetworkSetup
      {
         Name = "TerracoinTest",
         RootFolderName = "terracointest",
         CoinTicker = "TTRC",
         DefaultPort = 18321,
         DefaultRPCPort = 18322,
         DefaultAPIPort = 18322,
         PubKeyAddress = 111,
         ScriptAddress = 196,
         SecretAddress = 239,
         GenesisTime = --genesis-nonce-test,
         GenesisNonce = 4834,
         GenesisBits = 0x,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "",
         HashMerkleRoot = "",
         DNS = new[] { "seedtest1.trc.blockcore.net", "seedtest2.trc.blockcore.net", "seedtest.trc.blockcore.net" },
         Nodes = new[] { "104.238.156.46", "107.170.238.241" },
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
