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
        public const string ConfigFileName = "molie.conf";

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
            public const string RootFolderName = "molie";
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
                new DNSSeedData("seed1", "mol.molie.net"),
                new DNSSeedData("seed2", "mol1.molie.net"),
                new DNSSeedData("seed3", "mol2.molie.net"),
                new DNSSeedData("seed4", "mol3.molie.net"),
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
                { 0, new CheckpointInfo(new uint256("0x000002a1ad0e9fa339c1074f97f7f7de25dac50865966c6d8d8a075026373a5c"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                { 10, new CheckpointInfo(new uint256("0x4d1c528735266dbf999d070b5a18f54e535f87084bbe92e8febaa5bb2db6c74b"), new uint256("0xd50af11c4d301bfec5a4b5d1f46e0a751aac640da1bbbc31ac0db1af5b585f44")) },
                { 100, new CheckpointInfo(new uint256("0xd2fe88d6e4f56a4b55a27e3fc8e0257adc4497e5ec5ccaa64075b9ff007d3eff"), new uint256("0x2c783e2d5c82e4f42c7def4451e8236cf44ef7594fa37da3f7ca65ebb66911e2")) },
                { 1000, new CheckpointInfo(new uint256("0x994bda728098cfa6b815d8cf5bd29aa3d0b1d6a4e421b9d7460b56c5e7ecb9a2"), new uint256("0x54676536898e94ec1fada28d45eb0b5ec4dfc9f7e6d8a4b999d3593ad53f5ab1")) },
                { 2000, new CheckpointInfo(new uint256("0x291652a0e4712729ea82f60dee6b0127f25d2271cf0216b924f858e30a97e291"), new uint256("0x0eb067d54218fb3f488ed67d97ee82bdc7c4641718948761bbff7e83a1217603")) },
                { 5000, new CheckpointInfo(new uint256("0xf1a228bc882042d190a674fe69b50f76da8649bfc6005b05bfcf3f12674ba724"), new uint256("0x90e53f8e32bfdfed191fbf4f3eb6b8ae0b9f797071a9cc457c9698ee7bd5d684")) },
                { 10000, new CheckpointInfo(new uint256("0xe496c00c50b0176c6695ed303d4dcb44ed78ea16221fece64cc86054c0243630"), new uint256("0x38a01ba181957490efd395169f66a2fc62ac525ca584a0f54bd239abf6e0f931")) },
                { 20000, new CheckpointInfo(new uint256("0x6db25eeb5034b2831635abe5e986f86f7dd41064c6840bd5e141c3d28c2c3396"), new uint256("0xb264ac835c1c5fee9bc4ef7712d01ca652cd0a966e363e574998b58d245ee9f8")) },
                { 30000, new CheckpointInfo(new uint256("0x73c188629decfb81a4236d2f7c581a855aa9d7aa4b44533b7311a23f35a11ef7"), new uint256("0xd6ca6c00e58d47919d4b4c06711887664be3d1a753ca1af3e6a02486a525f4ce")) },
                { 50000, new CheckpointInfo(new uint256("0xf55e286d695b6e28dc1cea55db23e2c8f4e282db4d58f3cef2a9963785d760eb"), new uint256("0xb7ca872b75e9ee41a6d1f848fd67847211d31b9b3bfefce1a829e21db30394e6")) },
                { 100000, new CheckpointInfo(new uint256("0x4cba2e2b952ecf0f3671c17d4468800005db24d117315d1cff9cefbad5e85a79"), new uint256("0x34f0dfd00940e7724a4c89773775fc039e8d37155f29ea7cd1133e3e1eb4826a")) },
                { 150000, new CheckpointInfo(new uint256("0x9da6c50706dccb8eddee2dbf8bfd401be6d991e4b65bd09bbfd8fcd9f3e14214"), new uint256("0xd110b60a280b829bd370fc5a9388345c835db61638507ba56304d9a5c59c922c")) },
                { 200000, new CheckpointInfo(new uint256("0xfc9380c469d3f09aa0638ce6f6b2344322694c0958cbb7b36e80ee0e993b68d1"), new uint256("0x66156e9f4f09d69a3cee9bed6ddbdf0deac42251fa77b82c1dac558fef668275")) },
                { 300000, new CheckpointInfo(new uint256("0x398986ca2d89f7c267253e5d814663b56962fdf94f87a26c1311ec15638d81bb"), new uint256("0x50e45783c89c58cdd7a33f4823cdc49aad266d3ac110ac68825b2eea0a833bea")) },
                { 400000, new CheckpointInfo(new uint256("0x5811caef1f89a497f55135bbc244bdfc9f50c21ca9ff906f7762e81fb5eddeee"), new uint256("0x69daf3b1547b33a5b2b19ef28274054a31fcc879148c1cc4c29857e76bfb1966")) },
                { 500000, new CheckpointInfo(new uint256("0x91132a91aa24a6c2bebf90c02acbfe58def98cbab1072e0d3aec6f6873c3d7a9"), new uint256("0x902ee692a6a9f92d9eb32632f8e58399c5a4cbb256864b346e39965ea80c4135")) }
            };
        }


        internal class Test
        {
            public const string Name = "MolieTest";
            public const string RootFolderName = "molietest";
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
                new DNSSeedData("seed1", "mol.molie.net"),
                new DNSSeedData("seed2", "mol1.molie.net"),
                new DNSSeedData("seed3", "mol2.molie.net"),
                new DNSSeedData("seed4", "mol3.molie.net"),
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
            public const string RootFolderName = "molieregtest";
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
                new DNSSeedData("seed1", "mol.molie.net"),
                new DNSSeedData("seed2", "mol1.molie.net"),
                new DNSSeedData("seed3", "mol2.molie.net"),
                new DNSSeedData("seed4", "mol3.molie.net"),
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
