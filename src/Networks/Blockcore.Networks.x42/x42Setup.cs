using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks.x42.Networks.Setup;
using NBitcoin;

namespace Blockcore.Networks.x42
{
    internal class x42Setup
    {
        internal static x42Setup Instance = new x42Setup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "x42",
            ConfigFileName = "x42.conf",
            Magic = "42-66-52-03",
            CoinType = 424242, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 10.5m * 1000000,
            PoWBlockReward = 0,
            PoSBlockReward = 20,
            LastPowBlock = 523,
            GenesisText = "On Emancipation Day, we are fighting to maintain our democratic freedom at various levels - https://www.stabroeknews.com/2018/opinion/letters/08/01/on-emancipation-day-we-are-fighting-to-maintain-our-democratic-freedom-at-various-levels/ | popó & lita - 6F3582CC2B720980C936D95A2E07F809",
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F,
            BlocksWithoutRewards = true,
            LastProofOfStakeRewardHeight = 12155230,
            ProofOfStakeRewardAfterSubsidyLimit = Money.Coins(2),
            SubsidyLimit = 400000,
            PoSVersion = 3
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "x42Main",
            RootFolderName = "x42",
            CoinTicker = "x42",
            DefaultPort = 52342,
            DefaultRPCPort = 52343,
            DefaultAPIPort = 42220,
            DefaultSignalRPort = 42222,
            PubKeyAddress = 75,
            ScriptAddress = 125,
            SecretAddress = 75 + 128,
            GenesisTime = 1533106324,
            GenesisNonce = 246101626,
            GenesisBits = 0x1e0fffff,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0x04ffe583707a96c1c2eb54af33a4b1dc6d9d8e09fea8c9a7b097ba88f0cb64c4",
            HashMerkleRoot = "0x6e3439a32382f83dee4f94a6f8bdd38908bcf0c82ec09aba85c5321357f01f67",
            DNS = new[] { "mainnet1.x42seed.host", "mainnetnode1.x42seed.host", "tech.x42.cloud", "x42.seed.blockcore.net" },
            Nodes = new[] { "34.255.35.42", "52.211.235.48", "63.32.82.169", "18.179.72.204", "15.188.129.215", "18.157.117.214" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x04ffe583707a96c1c2eb54af33a4b1dc6d9d8e09fea8c9a7b097ba88f0cb64c4"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) }, // Genisis
                { 2, new CheckpointInfo(new uint256("0x1a64847f52fce72763a9eaa99bed6a896556917cd16f491bbdec070b40514282"), new uint256("0xa55d7663540264e7ed1e7195ecd0050303187eaf9485edeec70806491b5a53d1")) }, // Premine
                { 523, new CheckpointInfo(new uint256("0x1ca01c02f5989a198433cbe83e0eb26d9166d6aaaa9c20d6b765d5bace7829f1"), new uint256("0xbf04ecd478d78d302aa65293dde85036954b76216b0812104315c8a5ad139525")) }, // Last POW Block
                { 20000, new CheckpointInfo(new uint256("0x79976dfc025e982239a0bd62099475e6abf839c73aba5805b5cbe4091744c09a"), new uint256("0x250690dd6f264565c5ce16d84d250d67eb940d084c253e4006cdba3091fd66b6")) },
                { 200000, new CheckpointInfo(new uint256("0xaa276a1c51c025ff1a21fd4b07bfa5d55effc173840e054dd851b20dbb1f2f17"), new uint256("0x63d4bc7b0272703e94ae79103970ea324dc85221e88a51c39a170744848c0cc7")) },
                { 300000, new CheckpointInfo(new uint256("0xff72e73ee8f87c0de9bf82c3bb758f4905c3e005493f2ed1faede7c120961750"), new uint256("0x2097fc9edfb8dfe287db45bbce820e168c50be32c9840b1cddf56b297011fc69")) },
                { 500000, new CheckpointInfo(new uint256("0x7f9a88ebb32f47090ec37a110c5a05c1162a604dfbfb69c8d492e771cdb63289"), new uint256("0x19db6890c5c934e883bc99eb197509b0a81f19faeefcf49fd7fa6dab83644bfb")) },
                { 800000, new CheckpointInfo(new uint256("0x981083b047ecf8157a8b2dc24e17ca8cfad01b4e2dabc314df97f3b64fdf37f5"), new uint256("0xf3f0a821801b32c73a7c4f42416ddad3f74b732bd517c968a9b93a33d3684e0b")) },
                { 1000000, new CheckpointInfo(new uint256("0x1f5900bc62ddc11a383f8602d744fab1afa1e1969f0bf7f6b1b161476739a35e"), new uint256("0xca5fcf25a5561ebc91c5624b7c5ff697060f8a613e53e7a7e90abac925324e39")) },
                { 1211700, new CheckpointInfo(new uint256("0x66e4752642fc1d97f38cd6ce88e92c102fcadb0919dd9d29f4296ba5c77a8de4"), new uint256("0x0547efa90b74bbd999aabe39bfd390c2bab2e1cecb8685ae6562d119748bc597")) }
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "x42RegTest",
            RootFolderName = "x42",
            CoinTicker = "Tx42",
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
            DNS = new[] { "seedregtest1.x42.blockcore.net", "seedregtest2.x42.blockcore.net", "seedregtest.x42.blockcore.net" },
            Nodes = new[] { "34.255.35.42", "52.211.235.48", "63.32.82.169" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "x42Test",
            RootFolderName = "x42",
            CoinTicker = "Tx42",
            DefaultPort = 62342,
            DefaultRPCPort = 62343,
            DefaultAPIPort = 42221,
            DefaultSignalRPort = 42223,
            PubKeyAddress = 65,
            ScriptAddress = 196,
            SecretAddress = 65 + 128,
            GenesisTime = 1591458972,
            GenesisNonce = 2433759,
            GenesisBits = new Target(new uint256("0000ffff00000000000000000000000000000000000000000000000000000000")),
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0xa92bf124a1e6f237015440d5f1e1999bdef8e321f2d3fdc367eb2f7733b17854",
            HashMerkleRoot = "0xd0695e2d2562e7054b599c053fad4a72997f2e9629a2f9760e57584cf850ae57",
            DNS = new[] { "testnet1.x42seed.host" },
            Nodes = new[] { "63.32.82.169", "35.155.194.159" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x11bd504102b42b24680d7b4f9b9e9521adc1b690253494d108193cdfcdd2ef0b"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) }, // Genisis
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
