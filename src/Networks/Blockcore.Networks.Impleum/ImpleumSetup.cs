using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus.Checkpoints;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.Networks.Impleum
{
    public class ImpleumSetup
    {
        public const string ConfigFileName = "impleumx.conf";

        /// <summary>
        /// ImpleumX cointype. For Impleum it was 769
        /// </summary>
        public const int CoinType = 770; // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md 

        public const decimal PremineReward = 8734567;
        public const decimal PoWBlockReward = 1;
        public const decimal PoSBlockReward = 1;
        public const long MaxSupply = 21000000;
        public const string GenesisText = "Bloomberg 11/30/2020 Bitcoin Is Winning the Covid-19 Monetary Revolution"; // Bloomberg, 2020-11-2
        public static TimeSpan TargetSpacing = TimeSpan.FromSeconds(45);
        public const uint ProofOfStakeTimestampMask = 0x0000000F; // 0x0000003F // 64 sec
        public const int PoSVersion = 4;

        internal class Main
        {
            public const string Name = "ImpleumXMain";
            public const string RootFolderName = "impleumx";
            public const string CoinTicker = "IMPLX";
            public const int DefaultPort = 18105;
            public const int DefaultRPCPort = 18104;
            public const int DefaultAPIPort = 18103;
            public const int PubKeyAddress = 76; // X https://en.bitcoin.it/wiki/List_of_address_prefixes
            public const int ScriptAddress = 141; // y or z
            public const int SecretAddress = PubKeyAddress + ScriptAddress;
            public static readonly uint Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("ImpX")); //1483763017
            public const int LastPowBlock = 675;

            public const uint GenesisTime = 1607706917; // ~11 December 2020 - https://www.unixtimestamp.com/
            public const uint GenesisNonce = 1752306; // Set to 1 until correct value found
            public const uint GenesisBits = 0x1E0FFFFF; // The difficulty target
            public const int GenesisVersion = 1; // 'Empty' BIP9 deployments as they are all activated from genesis already
            public static Money GenesisReward = Money.Zero;
            public const string HashGenesisBlock = "0x000002e1a5c2361c43f5d76b7d77cd52c2866b391c59867ad79de49795ed7361";
            public const string HashMerkleRoot = "0x22ef4ec3f51e4b1d8266bb3dcff45b32647a751085d7174d68f7d3ff654206bf";

            public static List<DNSSeedData> DNS = new List<DNSSeedData>
            {
                new DNSSeedData("seed1", "mn1.uh420058.ukrdomen.com"),
                new DNSSeedData("seed2", "mn2.uh420058.ukrdomen.com"),
                new DNSSeedData("seed3", "mn3.uh420058.ukrdomen.com"),
                new DNSSeedData("seed4", "mn4.uh420058.ukrdomen.com"),
                new DNSSeedData("seed5", "impleum.com"),
                new DNSSeedData("seed6", "seed1.impleum.com"),
                new DNSSeedData("seed7", "seed2.impleum.com"),
                new DNSSeedData("seed8", "seed3.impleum.com"),
                new DNSSeedData("seed9", "seed4.impleum.com")
            };

            public static List<NetworkAddress> Nodes = new List<NetworkAddress>
            {
                //    new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
                //   new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort)
            };

            public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
            {
            { 0, new CheckpointInfo(new uint256("0x000002e1a5c2361c43f5d76b7d77cd52c2866b391c59867ad79de49795ed7361"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                 { 675, new CheckpointInfo(new uint256("0x228fe79897e110963fb1c39fc87f8627b51b37151d2184178139960b9ddc902a"), new uint256("0xb6191b4b733d38c0d3a7d1bcf95009bc8b173fc9dd435165bf08cde2eef33918")) },
                 { 1000, new CheckpointInfo(new uint256("0x6637657c16eb9ce7b99d09f41c0e12054dc0c47fd566f69af7778770bd5107f9"), new uint256("0x00eb64d91bb3dade131e3baf57fd40120586c0b08eea1852a2268b4eb4d87787")) },
                 { 1500, new CheckpointInfo(new uint256("0x21215e41cfa79a83b32c34419c9ff24712d5be48e2753ea58c80422432198f93"), new uint256("0x23e3130f9b7d4101a129c03792ac565f0850a6df475ac59bae77ae4761c6dc98")) },
                 { 2000, new CheckpointInfo(new uint256("0x6ae90bfcb5d1b6f1da5144f5f6e496fe9ab7e799f6fabffdeb4a4d30e50bb023"), new uint256("0x246398d97a66f88ac66319d38814d4a5369b7735aa595ad11f00e40ea4622d9a")) },
                 { 2500, new CheckpointInfo(new uint256("0x6857ebfe3ee8fdad274a6ae9baa3eb92f0fa5e2b99015eb9db526e52a4ea93ee"), new uint256("0xe8712f66a3d334ad4fd2b4d5a533de98d7acc6df8065ba55e46812e5c7066f4d")) },
                 { 5000, new CheckpointInfo(new uint256("0x1c246ba592df8017204016ca22ec07cde07750108e2ddabaa9e42235e6e24b2c"), new uint256("0x18b5001c53a2df968c266a243ea31cefa9b90681a64a7f11aea0cf2637261215")) },
                 { 10000, new CheckpointInfo(new uint256("0xf031eb6405232137e6fe1a44537c64f44d0991d6c9e42b16bab5b284029b1d7e"), new uint256("0x8a66e739b226a6752cd193aab96095084368bb3c36195bf1a7d8a41f81109e8f")) },
                 { 20000, new CheckpointInfo(new uint256("0xb24fca13c138617da441b4a983e582557cda12fb778d7fae0994af01e3f3d252"), new uint256("0x35c3f5856f36fdc43ee7da31530e649966b924cf59e1fddd87d7ed7837b63073")) },
                 { 30000, new CheckpointInfo(new uint256("0x8e7f225cb9eed57d578a6edf98a61f4120d20f0b41032eef35b557c2ae3b1c5e"), new uint256("0xd204fc2c31a15f5e3c02a178d100d88d1c10d21087c7a321594590f471ea6e51")) },
                 { 50000, new CheckpointInfo(new uint256("0x6d0541ad988fb15b9b59a291f0beca1cd147dcc60e7503d4610f21aedeee4053"), new uint256("0x9fdfb977228155cb14c814e463f6280ece1961e873165dd9eadabbc523ee35fe")) },
                 { 70000, new CheckpointInfo(new uint256("0x3f3d906896235395d6e203a1967875ba9408cc56d9a49b75503ef507712a7642"), new uint256("0x989fe82bec93afd796121443e62dab9a20fdb81764f5b52fceff05b05b4a781a")) }
            };
        }


        internal class Test
        {
            public const string Name = "ImpleumXTest";
            public const string RootFolderName = "impleumxTest";
            public const string CoinTicker = "TIMPLX";
            public const int DefaultPort = 28105;
            public const int DefaultRPCPort = 28104;
            public const int DefaultAPIPort = 28103;
            public const int PubKeyAddress = 104;
            public const int ScriptAddress = 129;
            public const int SecretAddress = PubKeyAddress + ScriptAddress;
            public static readonly uint Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("TmpX"));
            public const int LastPowBlock = 12500;

            public const uint GenesisTime = 1607707102;
            public const uint GenesisNonce = 117;
            public const uint GenesisBits = 0x1F0FFFFF;
            public const int GenesisVersion = 1;
            public static Money GenesisReward = Money.Zero;
            public const string HashGenesisBlock = "0x000155d1942054636a5a5ad37e9c7aa79bb0a430dea7017e1d3ed9e1be535e45";
            public const string HashMerkleRoot = "0x63ef070e33a794ea56ce116970b7779714364e4641d27ce78d0bb868f5ffca2d";

            public static List<DNSSeedData> DNS = new List<DNSSeedData>
            {
                new DNSSeedData("impleum.com", "impleum.com")
                //new DNSSeedData("seedtest2.impl.blockcore.net", "seedtest2.impl.blockcore.net"),
                //new DNSSeedData("seedtest.impl.blockcore.net", "seedtest.impl.blockcore.net")
            };

            public static List<NetworkAddress> Nodes = new List<NetworkAddress>
            {
                //new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
                //new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort),
            };

            public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x000155d1942054636a5a5ad37e9c7aa79bb0a430dea7017e1d3ed9e1be535e45"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
            };
        }

        internal class RegTest
        {
            public const string Name = "ImpleumXRegTest";
            public const string RootFolderName = "impleumxRegTest";
            public const string CoinTicker = "TIMPLX";
            public const int DefaultPort = 38105;
            public const int DefaultRPCPort = 38104;
            public const int DefaultAPIPort = 38103;
            public const int PubKeyAddress = 104;
            public const int ScriptAddress = 129;
            public const int SecretAddress = PubKeyAddress + ScriptAddress;
            public static readonly uint Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("RmpX"));
            public const int LastPowBlock = 12500;

            public const uint GenesisTime = 1607707102;
            public const uint GenesisNonce = 5729;
            public const uint GenesisBits = 0x1F00FFFF;
            public const int GenesisVersion = 1;
            public static Money GenesisReward = Money.Zero;
            public const string HashGenesisBlock = "0x00009954df399c3dcc5a627366de50a5015c22d01d64280275a30948faa5c01b";
            public const string HashMerkleRoot = "0x63ef070e33a794ea56ce116970b7779714364e4641d27ce78d0bb868f5ffca2d";

            public static List<DNSSeedData> DNS = new List<DNSSeedData>
            {
                //new DNSSeedData("seedregtest1.impl.blockcore.net", "seedregtest1.impl.blockcore.net"),
                //new DNSSeedData("seedregtest2.impl.blockcore.net", "seedregtest2.impl.blockcore.net"),
                //new DNSSeedData("seedregtest.impl.blockcore.net", "seedregtest.impl.blockcore.net"),
            };

            public static List<NetworkAddress> Nodes = new List<NetworkAddress>
            {
                //new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
                //new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort)
            };

            public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            };
        }

        public static bool IsPoSv3() => PoSVersion == 3;

        public static bool IsPoSv4() => PoSVersion == 4;

    }
}
