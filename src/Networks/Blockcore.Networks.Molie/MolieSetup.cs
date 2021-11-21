using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus.Checkpoints;
using Blockcore.P2P;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.Networks.Molie
{
    public class MolieSetup
    {
        public const string ConfigFileName = "Molie.conf";

        /// <summary>
        /// Molie cointype. For Molie it was 769
        /// </summary>
        public const int CoinType = 772; // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md 

        public const decimal PremineReward = 1049966250;
        public const decimal PoWBlockReward = 50;
        public const decimal PoSBlockReward = 0;
        public const long MaxSupply = 10500000000; 
        public const string GenesisText = "Multifunctional decentralized messenger and multi-currency wallet"; 
        public static TimeSpan TargetSpacing = TimeSpan.FromSeconds(45);
        public const uint ProofOfStakeTimestampMask = 0x0000000F; // 0x0000003F // 64 sec
        public const int PoSVersion = 4;

        internal class Main
        {
            public const string Name = "MolieMain";
            public const string RootFolderName = "Molie";
            public const string CoinTicker = "MOL";
            public const int DefaultPort = 22105;
            public const int DefaultRPCPort = 22104;
            public const int DefaultAPIPort = 22103;
            public const int PubKeyAddress = 51; // X https://en.bitcoin.it/wiki/List_of_address_prefixes
            public const int ScriptAddress = 141; // y or z
            public const int SecretAddress = PubKeyAddress + ScriptAddress;
            public static readonly uint Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("Molie")); //1483763017
            public const int LastPowBlock = 675;

            public const uint GenesisTime = 1619257421; // ~11 December 2020 - https://www.unixtimestamp.com/
            public const uint GenesisNonce = 222768; // Set to 1 until correct value found
            public const uint GenesisBits = 0x1E0FFFFF; // The difficulty target
            public const int GenesisVersion = 1; // 'Empty' BIP9 deployments as they are all activated from genesis already
            public static Money GenesisReward = Money.Zero;
            public const string HashGenesisBlock = "0x000002a1ad0e9fa339c1074f97f7f7de25dac50865966c6d8d8a075026373a5c";
            public const string HashMerkleRoot = "0x9f9930c2112eb4fc90d4abfa4085708d2e913c8cd638335be715e48601292bed";

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
           
                //{ 0, new CheckpointInfo(new uint256("0x000002e1a5c2361c43f5d76b7d77cd52c2866b391c59867ad79de49795ed7361"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                 
            };
        }


        internal class Test
        {
            public const string Name = "MolieTest";
            public const string RootFolderName = "MolieTest";
            public const string CoinTicker = "TMOL";
            public const int DefaultPort = 32105;
            public const int DefaultRPCPort = 32104;
            public const int DefaultAPIPort = 32103;
            public const int PubKeyAddress = 105;
            public const int ScriptAddress = 129;
            public const int SecretAddress = PubKeyAddress + ScriptAddress;
            public static readonly uint Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("Molie"));
            public const int LastPowBlock = 12500;

            public const uint GenesisTime = 1619257450;
            public const uint GenesisNonce = 1127;
            public const uint GenesisBits = 0x1F0FFFFF;
            public const int GenesisVersion = 1;
            public static Money GenesisReward = Money.Zero;
            public const string HashGenesisBlock = "0x000e5a32f82051916e14cdb9ccc4c7146c106349a0622df5d122410934554912";
            public const string HashMerkleRoot = "0x15a4348986e906634ff05ae5cdf208d0c297b3542b86a2919cbf03c7b5bddace";

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
                //new NetworkAddress(IPAddress.Parse("109.108.77.134"), DefaultPort),
                //new NetworkAddress(IPAddress.Parse("62.80.181.141"), DefaultPort),
            };

            public static Dictionary<int, CheckpointInfo> Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                
            };
        }

        internal class RegTest
        {
            public const string Name = "MolieRegTest";
            public const string RootFolderName = "MolieRegTest";
            public const string CoinTicker = "TMOL";
            public const int DefaultPort = 42105;
            public const int DefaultRPCPort = 42104;
            public const int DefaultAPIPort = 42103;
            public const int PubKeyAddress = 105;
            public const int ScriptAddress = 129;
            public const int SecretAddress = PubKeyAddress + ScriptAddress;
            public static readonly uint Magic = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("Rolie"));
            public const int LastPowBlock = 12500;

            public const uint GenesisTime = 1619257448;
            public const uint GenesisNonce = 11588;
            public const uint GenesisBits = 0x1F00FFFF;
            public const int GenesisVersion = 1;
            public static Money GenesisReward = Money.Zero;
            public const string HashGenesisBlock = "0x000083c58f9fc90470670c6ccee600f8db5837fc38f6b0d19d3984234d2ec8e3";
            public const string HashMerkleRoot = "0x81ff297a766f945a1c4c536969900ca4fa5f3bd359d567ada6adb1accf063626";

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
