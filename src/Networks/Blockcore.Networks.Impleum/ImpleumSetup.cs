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
                new DNSSeedData("seed4", "mn4.uh420058.ukrdomen.com")
            };

            public static List<NetworkAddress> Nodes = new List<NetworkAddress>
            {
                //    new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
                //   new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort)
            };

            public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x000002e1a5c2361c43f5d76b7d77cd52c2866b391c59867ad79de49795ed7361"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
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
