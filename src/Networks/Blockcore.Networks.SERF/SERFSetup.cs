using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.SERF;
using Blockcore.Networks.SERF.Setup;
using NBitcoin;

namespace Blockcore.Networks.SERF
{
   internal class SERFSetup
   {
      internal static SERFSetup Instance = new SERFSetup();

      internal CoinSetup Setup = new CoinSetup
      {
         FileNamePrefix = "serf",
         ConfigFileName = "serf.conf",
         Magic = "01-4F-4E-4D",
         CoinType = 712, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
         PremineReward = 1000000,
         PoWBlockReward = 8,
         PoSBlockReward = 2,
         LastPowBlock = 10000000,
         GenesisText = "15-06-1215 - JOHN, by the grace of God King of England", // The New York Times, 2020-04-16
         TargetSpacing = TimeSpan.FromSeconds(120),
         ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
         PoSVersion = 3
      };

      internal NetworkSetup Main = new NetworkSetup
      {
         Name = "SERFMain",
         RootFolderName = "serf",
         CoinTicker = "SERF",
         DefaultPort = 15111,
         DefaultRPCPort = 15112,
         DefaultAPIPort = 15113,
         PubKeyAddress = 63, // S https://en.bitcoin.it/wiki/List_of_address_prefixes
         ScriptAddress = 110, 
         SecretAddress = 160,
         GenesisTime = 1617485759,
         GenesisNonce = 398649,
         GenesisBits = 0x1E0FFFFF,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "00000703afaab42ce9a69557581a27b784aa212d3485b7a25c207571e4fad9a9",
         HashMerkleRoot = "377be49b87dfc41936a0518dde6e3b712030b2c2fbf834fa92605e0a90054d55",
         DNS = new[] { "seed1.serfnet.info", "seed2.serfnet.info", "serf.seed.blockcore.net" },
         Nodes = new[] { "45.76.123.202", "45.32.246.83", "78.141.230.15" },
         Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         }
      };

      internal NetworkSetup RegTest = new NetworkSetup
      {
         Name = "SERFRegTest",
         RootFolderName = "serfregtest",
         CoinTicker = "TSERF",
         DefaultPort = 25111,
         DefaultRPCPort = 25112,
         DefaultAPIPort = 25113,
         PubKeyAddress = 111,
         ScriptAddress = 196,
         SecretAddress = 239,
         GenesisTime = 1617485803,
         GenesisNonce = 159764,
         GenesisBits = 0x1F00FFFF,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "00008ac6bc8a76d43e05d4411bed6cf259a5a2054c9b92f0f327220234c0768d",
         HashMerkleRoot = "ed8306578242746d4104fd128962d3b1851242d42e994cf2a64c013945073203",
         DNS = new[] { "seedregtest1.serf.blockcore.net", "seedregtest2.serf.blockcore.net", "seedregtest.serf.blockcore.net" },
         Nodes = new[] { "45.76.123.202", "45.32.246.83", "78.141.230.15" },
         Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            // TODO: Add checkpoints as the network progresses.
         }
      };

      internal NetworkSetup Test = new NetworkSetup
      {
         Name = "SERFTest",
         RootFolderName = "serftest",
         CoinTicker = "TSERF",
         DefaultPort = 35111,
         DefaultRPCPort = 35112,
         DefaultAPIPort = 35113,
         PubKeyAddress = 111,
         ScriptAddress = 196,
         SecretAddress = 239,
         GenesisTime = 1617485822,
         GenesisNonce = 2523,
         GenesisBits = 0x1F0FFFFF,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "000267bc3ae045a9681dcc7fc51c5cc8ede55b70fbd8f574636cde21f6510772",
         HashMerkleRoot = "c6bf379120ff9adbb2b7b14df1d719161fe588337bbcd1f6a947fefabb7a7214",
         DNS = new[] { "seedtest1.serf.blockcore.net", "seedtest2.serf.blockcore.net", "seedtest.serf.blockcore.net" },
         Nodes = new[] { "45.76.123.202", "45.32.246.83", "78.141.230.15" },
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
