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
         PoWBlockReward = 10,
         PoSBlockReward = 8,
         LastPowBlock = 200,
         GenesisText = "15-06-1215 - JOHN, by the grace of God King of England", // The New York Times, 2020-04-16
         TargetSpacing = TimeSpan.FromSeconds(120),
         ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
         PoSVersion = 4
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
         GenesisTime = 1617798448,
         GenesisNonce = 459711,
         GenesisBits = 0x1E0FFFFF,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "000000aa4f0a6d11fe3fa3486ce79440fc3884292a33352571bbd56c691805fa",
         HashMerkleRoot = "bc3eb76540ed954702de5c1ddd31e6ea6172679634896a0775b8d2b54c841d5d",
         DNS = new[] { "seed1.serfnet.info", "seed2.serfnet.info", "serf.seed.blockcore.net" },
         Nodes = new[] { "45.76.123.202", "45.32.246.83", "78.141.230.15" },
         Checkpoints = new Dictionary<int, CheckpointInfo>
         {
            { 0, new CheckpointInfo(new uint256("0x000000aa4f0a6d11fe3fa3486ce79440fc3884292a33352571bbd56c691805fa"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
            { 2, new CheckpointInfo(new uint256("0x74535a5b45531321c34a0fd88e6eb824f1a2bbc99b14c742de173ea934ab5eeb"), new uint256("89d946daf1138a0e309f9ecdc2b642dccba94f8d164dcba563cd7ba4339ad487")) },
            { 50, new CheckpointInfo(new uint256("0x21315c0358823e1886c2d29fb4159679deea028b553b8c88cc998b25eb1ba69c"), new uint256("81e9b9020dd6a7d4215a82047368554008bb39a0b78bb311a0b39092163d54cb")) },
            { 100, new CheckpointInfo(new uint256("0x8e95d680c5acfe869fab695f570435bdf98ca7cf637e214b79da9385d776ec38"), new uint256("009101413058edc4363dcfadc556470d53d3060df68fb4f4f59e2266237e6ae1")) },
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
         GenesisTime = 1617798497,
         GenesisNonce = 7498,
         GenesisBits = 0x1F00FFFF,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "0000f7cb7de14863d087c9535440595d042f66d921a729fcf5e322022548a4be",
         HashMerkleRoot = "4b41bcf90223402fc7dc3595edc22a4d2eed8260e223c1df53e73a97e03598bf",
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
         PubKeyAddress = 64,
         ScriptAddress = 196,
         SecretAddress = 239,
         GenesisTime = 1617798497,
         GenesisNonce = 813,
         GenesisBits = 0x1F0FFFFF,
         GenesisVersion = 1,
         GenesisReward = Money.Zero,
         HashGenesisBlock = "000496ef72f9d2c0d84131c78e4770fa77fa0c1455794bfe1e8d4be0416062bc",
         HashMerkleRoot = "4b41bcf90223402fc7dc3595edc22a4d2eed8260e223c1df53e73a97e03598bf",
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
