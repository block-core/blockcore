using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using Rutanio.Networks;
using Rutanio.Networks.Setup;
using NBitcoin;

namespace Rutanio
{
    internal class RutanioSetup
    {
        internal static RutanioSetup Instance = new RutanioSetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "Rutanio",
            ConfigFileName = "rutanio.conf",
            Magic = "26-75-52-09",
            CoinType = 462, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 700000000,
            PoWBlockReward = 200,
            PoSBlockReward = 20,
            LastPowBlock = 45000,
            GenesisText = "https://edition.cnn.com/2019/07/28/sport/tour-de-france-bernal-colombia-spt-intl/index.html", 
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
            PoSVersion = 3
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "RutanioMain",
            RootFolderName = "rutanio",
            CoinTicker = "RUTA",
            DefaultPort = 6782,
            DefaultRPCPort = 6781,
            DefaultAPIPort = 39220,
            DefaultSignalRPort = 39720,
            PubKeyAddress = 60,  // R https://en.bitcoin.it/wiki/List_of_address_prefixes
            ScriptAddress = 122, // r
            SecretAddress = 188,
            GenesisTime = 1564350120,
            GenesisNonce = 288859,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0x00000347c656a618f9bfef80a14fa66cf26e34ed4caeba0e3f072eb8b9408ee6",
            HashMerkleRoot = "0xa74f9cb5ad97977b1e1079658f8290aa1e6122ee50df327a7e39480f94237c54",
            DNS = new[] { "seednode1.rutax.cloud", "seednode2.rutax.network", "seednode3.rutax.cloud", "seednode4.rutax.network" },
            Nodes = new[] { "vps301.rutax.cloud", "vps302.rutax.network", "vps303.rutax.cloud", "vps304.rutax.network" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                 { 0, new CheckpointInfo(new uint256("0x00000347c656a618f9bfef80a14fa66cf26e34ed4caeba0e3f072eb8b9408ee6"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) }, //Premine
                 { 2, new CheckpointInfo(new uint256("0xf9364e84a73f90efd930971793827c47bff0f695af13c6e8e7a11de7584e0b8a"), new uint256("0xed21b9d9a1c92570b84568a0c187f60e5da335558bb1bc1afa835e8793daa32b")) },
                 { 10, new CheckpointInfo(new uint256("0x72d855332d090a51371e45f19ae7b766abbe56a9589bcf287ca5aed5d20985c9"), new uint256("0x5b49b430c502a8e60718806c2e55116d3e375bf21a71c57de25ea17fb7da2f9c")) },
                 { 50, new CheckpointInfo(new uint256("0xe2322556f4d5a05f2bb4497c5a9647bcf14d8463fd27ac299592428be4336914"), new uint256("0xc9f357c180c59fb09fda2810e0237bff2b2d4e1a7cb4a6230ed628336b5614a1")) },
                 { 100, new CheckpointInfo(new uint256("0x566e196f627670a230cfc3e2c4ecdc12f92e53e81d32b0eb3ec5d2df4156eece"), new uint256("0x18069bbf9bf450cc6aec66f306183ca2289ef14e72909047d90b6bff0ea1d0c7")) },
                 { 500, new CheckpointInfo(new uint256("0x09920e672b839b9c20eb08136a47c1d35ffdc645e8b66258d4f19fc5ea6546e8"), new uint256("0x602395ff9a4e052675f58e982df9cc32c61a393162287dcbfd5635df73a1d0f6")) },
                 { 1000, new CheckpointInfo(new uint256("0xf27293554eb63269388c8833c86ac31adb18b4b3ad3f31bb15090914461ff988"), new uint256("0x0acec54f0754e5057ad9f98b59556b6c4ca1451927788fc13e1106b4375147c8")) },
                 { 2000, new CheckpointInfo(new uint256("0xafb6da8a473b613a737862e2f16f11baf53006693d1dce44ab00c599893f195f"), new uint256("0xe4c49686ba6bb2709ad556503d900f93839f8ccec9798e4e671783a3fcd3e107")) }, // 1.0.0.1 RutanioD
                 { 5000, new CheckpointInfo(new uint256("0x10755a3fc2da7f4c228528dea676de17a6aa89d03cf108f9d823c5419a2201e1"), new uint256("0xcece2418b82cce7f7e77326e4bbf2dbed42afc57b237d2c3118cbd6d45880f8e")) }, 
                 { 10500, new CheckpointInfo(new uint256("0x59001bcb08767fa93cc0ad20b84da1fc0843fd1d8d8c7bc6c196c6cc49236c15"), new uint256("0xbaa0f06120b60fb31938812a86e1d4b65608303b4c803f845643937e1d67d6b8")) }, // 1.0.0.2 RutanioD
                 { 13000, new CheckpointInfo(new uint256("0x6b15c3ce6cb6af2a4b93a74ebb63fa3e2487dad9913ea513159ba9ff019b84d4"), new uint256("0x20d1c7c623700d012a71e7ba456b9ecdd0a3f2441f2b2f677fa8f3360d9ca797")) },
                 { 16000, new CheckpointInfo(new uint256("0x754370ee32607b3037d7e402b6e47df06fadc4664afa8d6e2575df3bc7271d8c"), new uint256("0x229c9cc1148c6018bb1ef3bed59c37a0e82860f398479587f4546d5bdc2759dc")) },
                 { 19000, new CheckpointInfo(new uint256("0x3df9f229ff5092077859b4cf67451dfccc8cec4b828f388a71ecce43329225f5"), new uint256("0xfcd39f9a71bfe492d230812b3d4974b547f292ad176af26f2c9212052a2483ba")) }, // 1.0.0.3 Rutandiod
                 { 25000, new CheckpointInfo(new uint256("0xc6fc0ed3359d51f215d6facb6e0e61093633d607f27332a9cf8c2ade665f61e9"), new uint256("0xb96cca7f2710b8f9250536c24af2d0660eb1f44937a190a84752baacee152e32")) },
                 { 30000, new CheckpointInfo(new uint256("0x30d549c4fd940efd3abe88b4366527bc53fdba9197ede8a3d4a537e612e4311e"), new uint256("0x814b5e6f8800f4f6b48a99e901b465ff3de5ae48c70d5052371aa7833c6aa3a9")) },
                 { 35000, new CheckpointInfo(new uint256("0x7e530128063b4e8e4691e3de08fc68f3f98278a25c8b31f52dd3257fbd352ffd"), new uint256("0x2616e20eb0f57658a245662a71c8c72535af7918328199358a79d3708c44e1ab")) },
                 { 39000, new CheckpointInfo(new uint256("0x8ec08b1c01c5c885d2272a6ab8dd887f2d440b309929947ac59cbd500e6663c7"), new uint256("0x13c01277ecfcaa58b39a42eafade65bf3951d8a308a264fd5274f664b8eaa083")) }, // v1.0.0.4
                 { 50000, new CheckpointInfo(new uint256("0x5007111c94a91fc08f86a3d4eb23e9c8bf71dd9a0dec50eb46695913b1cd9c89"), new uint256("0xa748def00e9e5bf99fd1456b8b816897822ae704436ec1d6cf5c91ed0b368d3f")) },
                 { 100000, new CheckpointInfo(new uint256("0xdb024c71b8abceee321230b695e60f5bc6342d0168f8bdda639e08a7a1e149b8"), new uint256("0xbb966e8df430edcc1c591ecc3821bb766213212c274f4435c305142588bb28d2")) },
                 { 150000, new CheckpointInfo(new uint256("0xcab87f808e59ff30e8c741df899f993bb9b30cc78f0438a6c1ffac04abf9f124"), new uint256("0x201fd57cd71bbcf604ecb73a9770ded4210b046d857d5ce7865f2fa0e5e5c98e")) },
                 { 200000, new CheckpointInfo(new uint256("0xf1c63ee81dbdd9a419d75d74c1bfcef1a1d135745bf83d3c263ee77f0533d0bf"), new uint256("0x44c40d7991b88d56eea10154e32cd5c10087094f4f831b79be2c5dcfb085ee71")) },
                 { 309000, new CheckpointInfo(new uint256("0x7e24a192fdfc29290a831cdc10a6adcfb55dfb457de1b5ed00d0a13ffa2ead17"), new uint256("0x01c332d8310b5d165aa64cd0a70849143c23b04b2689a5523d1f7dd52f670bc6")) },
                 { 475000, new CheckpointInfo(new uint256("0x502700a3ab205f8e7120dcfad7d982dc30b3067f529e712032c724d8a21d7482"), new uint256("0x6bff1fd82fd6c13799c614f44b8f785e7ba976d1ff082a165309bac6e72dfd43")) }
            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "RutanioRegTest",
            RootFolderName = "rutanioregtest",
            CoinTicker = "TRUTA",
            DefaultPort = 56782,
            DefaultRPCPort = 56781,
            DefaultAPIPort = 39222,
            DefaultSignalRPort = 39722,
            PubKeyAddress = 68,
            ScriptAddress = 199,
            SecretAddress = 196,
            GenesisTime = 1411111111,
            GenesisNonce = 232431,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "000001eaf5fc73c5a2ded387a256d79b34b0acaaead5767a52c8c6081b79d031",
            HashMerkleRoot = "d382311c9e4a1ec84be1b32eddb33f7f0420544a460754f573d7cb7054566d75",
            DNS = new[] { "seednoderegtest1.rutan.cloud"},
            Nodes = new[] { "vps301.rutan.cloud" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        //TODO: Update Rutanio parameters
        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "RutanioTest",
            RootFolderName = "rutaniotest",
            CoinTicker = "TRUTA",
            DefaultPort = 16782,
            DefaultRPCPort = 16781,
            DefaultAPIPort = 39222,
            DefaultSignalRPort = 39721,
            PubKeyAddress = 68,
            ScriptAddress = 199,
            SecretAddress = 196,
            GenesisTime = 1542885720,
            GenesisNonce = 5218499,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "000000a45fb1d49065d6bad6ca84e5130dc319e0db5003d14a6c201a5b2895a0",
            HashMerkleRoot = "88cd7db112380c4d6d4609372b04cdd56c4f82979b7c3bf8c8a764f19859961f",
            DNS = new[] { "seedtest1.rutan.cloud", "seedtest2.rutan.network", "seedtest3.rutan.cloud" },
            Nodes = new[] { "23.97.234.230", "13.73.143.193", "89.10.227.34" },
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