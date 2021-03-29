using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using OpenExo.Networks;
using OpenExo.Networks.Setup;
using NBitcoin;

namespace OpenExo
{
    internal class OpenExoSetup
    {
        internal static OpenExoSetup Instance = new OpenExoSetup();

        internal CoinSetup Setup = new CoinSetup
        {
            FileNamePrefix = "exos",
            ConfigFileName = "exos.conf",
            Magic = "28-62-48-76",
            CoinType = 248, // SLIP-0044: https://github.com/satoshilabs/slips/blob/master/slip-0044.md,
            PremineReward = 300000000,
            PoWBlockReward = 12,
            PoSBlockReward = 1,
            LastPowBlock = 45000,
            GenesisText = "http://www.bbc.com/news/world-middle-east-43691291",
            TargetSpacing = TimeSpan.FromSeconds(64),
            ProofOfStakeTimestampMask = 0x0000000F, // 0x0000003F // 64 sec
            PoSVersion = 3
        };

        internal NetworkSetup Main = new NetworkSetup
        {
            Name = "EXOSMain",
            RootFolderName = "exos",
            CoinTicker = "EXOS",
            DefaultPort = 4562,
            DefaultRPCPort = 4561,
            DefaultAPIPort = 39120,
            DefaultSignalRPort = 39620,
            PubKeyAddress = 28,  // C https://en.bitcoin.it/wiki/List_of_address_prefixes
            ScriptAddress = 87, // c
            SecretAddress = 156,
            GenesisTime = 1523205120,
            GenesisNonce = 842767,
            GenesisBits = 0x1E0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0x00000036090a68c523471da7a4f0f958c1b4403fef74a003be7f71877699cab7",
            HashMerkleRoot = "0x85c4a8a116eb457ff74bb64908e71c6780bff7e69ad3dadc9df6cd753c21f937",
            DNS = new[] { "seednode1.oexo.cloud", "seednode2.oexo.net", "seednode3.oexo.cloud", "seednode4.oexo.net" },
            Nodes = new[] { "vps101.oexo.cloud", "vps102.oexo.net", "vps201.oexo.cloud", "vps202.oexo.net" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                { 0, new CheckpointInfo(new uint256("0x00000036090a68c523471da7a4f0f958c1b4403fef74a003be7f71877699cab7"), new uint256("0x0000000000000000000000000000000000000000000000000000000000000000")) },
                { 2, new CheckpointInfo(new uint256("0x892f74fe6b462ec612dc745f19b7f299ffb94aabb17a0826a03092c0eb6f83ec"), new uint256("0x8e61edcdfee2e948c7b6885b91a853cb82ba22ae5a5ef97776f2fb381f99cb09")) },
                { 10, new CheckpointInfo(new uint256("0xd7c9e4381c4b9331be82d13e0b40b932ac624f3fa8b4e80fb8e9f22769095c0a"), new uint256("0x221232ad2caa1635d2b1302b6eafc0254255a7bf5a2d3ea25ac6ac91f6e1c256")) },
                { 50, new CheckpointInfo(new uint256("0x71b54f5cb27d2b0b1dd34ba89c7ff4fa8e8b37da727373cf98134069fb84803e"), new uint256("0x8aaf92c1d948ba107711997b9d56c08ace5e6d3ab11dfbf2ffffc19798eff454")) },
                { 100, new CheckpointInfo(new uint256("0xf1fbdb26299ca5bac77963cc2ffd9e1830c3f106b87b235b5c731b4b3259d383"), new uint256("0xfeef70b0e16e80bb2f977ebc3f9aa72e4a5c052d2825e9fe6ee8a593555760ea")) },
                { 500, new CheckpointInfo(new uint256("0x72bc96ba513fb55431f335de897bb8ce80d898e17a3d4d5e05c48d1670d5f234"), new uint256("0x5e39f38d0a7f68b0aa59c0a5317d6b9ac1c208f01fe56df079f1d0d9d3da3b09")) },
                { 1000, new CheckpointInfo(new uint256("0xf8f562cd694b5ca517aca2e1cd5b953c22a185e715209bfc2a6ae8eb0a524289"), new uint256("0xa1f55377eef150d1c6863d77c618cf397080ddcdfaa0da2038f1682019c83631")) },
                { 5000, new CheckpointInfo(new uint256("0x9fbc9dc45a507a287d043b43a55e0623aca4368ca5c655bc1a7a97eef4951d1a"), new uint256("0x77412fe812a6f4d59b8cce8f4bd2ac7e8bfb5dc56b542348e66f9a4bc7c00441")) },
                { 10000, new CheckpointInfo(new uint256("0x45e01c13af7625b7289ed12f687864d485f603c2b5dcccfff26789bcbbc20439"), new uint256("0x2ccca90aa37406865a6feb5ec198f01d045a64a2aca5ff56ac96c88fe37d2514")) },
                { 14000, new CheckpointInfo(new uint256("0xecd5ae5e58ddde01087a4c7f2033252acc7237a7aa958f4cd0eb016b3c11cd0e"), new uint256("0xd608bfd6f8b4be11f1a0acd50f0cd269b35d40b9b298c564b0a79b9771025c19")) },
                { 18000, new CheckpointInfo(new uint256("0x42ee388a72f85e8a63ed81bdaa4d87040a009cff8471f15e5711ab824faedaa7"), new uint256("0x912d1fcd0f1aa5217893cdda2c536cdbed6039af4cb3dc10a9e9f9f8e7522e6e")) },
                { 20000, new CheckpointInfo(new uint256("0xead9d788c5e0a275d9a8434487248d5b5ed1db14de8ea1627dd70ad1e5fb5f5b"), new uint256("0xcaa8a9f4374e56ef86743040f089e022c50a9cb6cdeb432d66bc077aaf9e8078")) },
                { 25000, new CheckpointInfo(new uint256("0x9e889f90ee0a249a84c2c090117be70de8d53466a5f0bb312fde435ad50080f0"), new uint256("0xd49c521e4c46fb798dd366afbec6c90020d295228e32f0582f9fd5ce2674f3a0")) },
                { 30000, new CheckpointInfo(new uint256("0xcd878a41441aca4e2903941c374d0caf17d7080fd1c5e37aca9caab63e82f333"), new uint256("0xa58e327073c17ed360cfe6747ac2fe9ec6bff3f5be00415b3d2897ccf39d5189")) },
                { 35500, new CheckpointInfo(new uint256("0x3196c8456be83bc810ef0f6e2b34a0962435e89d0844b9db8070487d5ec91afa"), new uint256("0x309d454e9906e40c9472762633188af3957d586be33994a7586c23accb99bd40")) },
                { 40000, new CheckpointInfo(new uint256("0x4d7b1d7115714d16dce3266087309e898e02a7dc1eb8d3f450c8473836de5a19"), new uint256("0x14eb31bf1e59918b45f74afe0d179475a3a83dfc11e33c974520e5567c8ffb8e")) },
                { 45000, new CheckpointInfo(new uint256("0x5f110ad2e1fbbb98bcd4d85167c7904631304bbd311144118f631d59795c0f00"), new uint256("0xf4171d8e19d71d032915363acef2a52f29c0833fe6d685fd0cf7d98efa2942bb")) },
                { 47000, new CheckpointInfo(new uint256("0x0b834c3a8d77939f5fc2e372bc03925a1170e8d386671a8d0f21fa8e5a9d440e"), new uint256("0x5d96f845bf42cc466e5857952fd5e00e27a51c21cdb8b4399f71da1ae4bbba2b")) },
                { 60000, new CheckpointInfo(new uint256("0xe904061cd883995fa96fa162927f771a3e0a834866cf741da1bf6f64d9807aab"), new uint256("0xd6778815c7b9989ea2ddba8d69e7b7c94a7cd48320b1295e5c23784b31b184d9")) },
                { 80000, new CheckpointInfo(new uint256("0x4991919485bc0c4f1f3b89dde06f90cca64b6eee2887a8f2105ed1f7219b056e"), new uint256("0x5f667b4d09a5e0e6fb6f41a7f220f4b769bc225c3feb6f826f84c1a2aedc144a")) },
                { 100000, new CheckpointInfo(new uint256("0x5be1f76165c7133562cdcb3beeac6413aea18e538883e379d5929c6eb26999d1"), new uint256("0x2e0dc1a4fce268f5a85b41a4a9e37032a26b77a6a22d7eae2df5dd4b1e004353")) },
                { 125000, new CheckpointInfo(new uint256("0xd2f245d326e4e0f1e9e22f548496ca9e47025721d7fece27e071a9f63628f0b8"), new uint256("0x6884864f6017c768868774e600907c104efd3b1e2c529411f7d88a1c11a0478e")) },
                { 150000, new CheckpointInfo(new uint256("0x4b4325e2c02654284de2719033c0defba485bd08a6259ca67372600447bf084e"), new uint256("0x358255ae11f9e061268ee9a4947ab275b4c0c12cc4d38b1a581801e1e85da1cb")) },
                { 200000, new CheckpointInfo(new uint256("0x4e4e40dc5cc007135f5113e1ebb22b06c39cff15637b5f51d93340a9cad0dfdf"), new uint256("0xc12688b1c2f4f95762ddac23d078b5a3b4fa02b5c351ad1f544839ff4ef5c061")) },
                { 250000, new CheckpointInfo(new uint256("0x20c97546de02e60c2d53a9c95e65956a3d89e81eb5f7075882fac2d6cc24d316"), new uint256("0x4e2533fd3cf6c03c1eeeea174df123d9a60d50b15e39ee39ea9450514c473731")) },
                { 300000, new CheckpointInfo(new uint256("0x1dca0bf2f051429e911fa9b232fcdf69bbaed667fa55451a3ff4d6450ae5dc52"), new uint256("0xf424bdc3c5ce706a531986bf5ace04ad29d8f141396e36f71ff873a3ec26bb09")) },
                { 350000, new CheckpointInfo(new uint256("0x8ab2fa51dc9c83200f3b2c662648f479f656635ce5c847209f74c2636d45e5c8"), new uint256("0x322303ee24efdb0a07524b95ad9949c278fd4a7aa2cd259f7ac611333b4bc792")) },
                { 400000, new CheckpointInfo(new uint256("0x78da79e80c94c2a175276eb4b903a15395d4213e8814d1f1fabc225e05c56e69"), new uint256("0xc7179fa14c296ebc661f2f1db32d4492cc176d86fe64ad14fb2058ac0f132294")) },
                { 450000, new CheckpointInfo(new uint256("0xfaa749cba84d1111affe61cb8c14ba43a452a265a136066554a22cfa14cabfcd"), new uint256("0x651884ceb30069212c90b5fb3a11d2e6263c0a5c76d6b9b92274ffd6cb3b8fea")) },
                { 600000, new CheckpointInfo(new uint256("0xa4bf14b94da99b72abafb66624e0f238bef68f9141f79f811f618d67bd8149d1"), new uint256("0x99199a7ac240838d58e362e834e43fc52b74e146f84ce3d879d4c213a4799745")) },
                { 790000, new CheckpointInfo(new uint256("0x11610eefff99c06e8fe4667adb07f8598c339ed04e6eed5414b115fb7d4cfd49"), new uint256("0x46cb091b43846e21b547ebf3787deab210628919421180d4cc8275d276fe1cda")) },
                { 900000, new CheckpointInfo(new uint256("0x960369823996b14b7f08b876e8f95378119831ce81a72886434c9fd41d04c580"), new uint256("0x019aec9d3716f63d5c645b860b752e1699f176eaa4830e2daeb4fa1831b119d1")) },
                { 1000000, new CheckpointInfo(new uint256("0x1a1fc554ec11796d1c98a674ebd419db9abeba14419b97964f3ce357986d0350"), new uint256("0x8f9b2589afadc9e1f56e6cc216f3feb267339a825b9fcbb36f25c90edac8f0bd")) },
                { 1300000, new CheckpointInfo(new uint256("0x260b7433a7c72694f57f2e0ea6821f5ab33afea3c567f9845c8a989eb443f9d4"), new uint256("0x79f20f2a22bb3c8a0808e6d5e0daa7d8ce0ecbefcd1d842fa2e5a23e14b22d45")) },
                { 1355000, new CheckpointInfo(new uint256("0x890101f0f9b4c16c39eafeca5aa744772b7612eb003583200a5836cbb0acb332"), new uint256("0x5c34eb0ef4759118e720bdc57a72ffe4d8d4048e2706d053f420e2a2c399fe6c")) },
                { 1401000, new CheckpointInfo(new uint256("0x9eb2e4d7650f401e5d27af3fd66848c3318e7a2a660d44b3c8196ab0c956e074"), new uint256("ff4fd825959265782056f501e68406dcc50e2fe9bc9f82d773f26a6318cf1529"))}

            }
        };

        internal NetworkSetup RegTest = new NetworkSetup
        {
            Name = "EXOSRegTest",
            RootFolderName = "exosregtest",
            CoinTicker = "TEXOS",
            DefaultPort = 15888,
            DefaultRPCPort = 15889,
            DefaultAPIPort = 39122,
            DefaultSignalRPort = 39622,
            PubKeyAddress = 75,
            ScriptAddress = 206,
            SecretAddress = 203,
            GenesisTime = 1391501792,
            GenesisNonce = 4249953,
            GenesisBits = 0x1F00FFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0x00000755a53922ad3443a7609ee700ca246a547783116f2085fff1e486e56085",
            HashMerkleRoot = "d382311c9e4a1ec84be1b32eddb33f7f0420544a460754f573d7cb7054566d75",
            DNS = new[] { "" },
            Nodes = new[] { "" },
            Checkpoints = new Dictionary<int, CheckpointInfo>
            {
                // TODO: Add checkpoints as the network progresses.
            }
        };

        //TODO: Update Rutanio parameters
        internal NetworkSetup Test = new NetworkSetup
        {
            Name = "EXOSTest",
            RootFolderName = "exostest",
            CoinTicker = "TEXOS",
            DefaultPort = 14562,
            DefaultRPCPort = 14561,
            DefaultAPIPort = 39121,
            DefaultSignalRPort = 39621,
            PubKeyAddress = 75,
            ScriptAddress = 206,
            SecretAddress = 203,
            GenesisTime = 1572376229,
            GenesisNonce = 40540,
            GenesisBits = 0x1F0FFFFF,
            GenesisVersion = 1,
            GenesisReward = Money.Zero,
            HashGenesisBlock = "0x00000bf810e65773b5a0e5a43ea656080e10108424dcf475abc4228bfc52148f",
            HashMerkleRoot = "88cd7db112380c4d6d4609372b04cdd56c4f82979b7c3bf8c8a764f19859961f",
            DNS = new[] { "testseednode1.oexo.cloud", "testseednode2.oexo.net", "testseednode3.oexo.cloud", "testseednode4.oexo.net" },
            Nodes = new[] { "vps151.oexo.cloud", "vps152.oexo.net", "vps251.oexo.cloud", "vps252.oexo.net" },
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
