using System.IO;
using System.IO.Compression;
using System.Linq;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.Networks;
using Blockcore.Networks.Bitcoin;
using Blockcore.Networks.Stratis;
using Blockcore.Tests.Common;
using NBitcoin;
using Xunit;

namespace Blockcore.IntegrationTests
{
    public class GenerateChain
    {
        private const string SkipTestMessage = "Not to be run as part of the unit test suite.";

        private const string MinerMnemonic = "elevator slight dad hair table forum maze feed trim ignore field mystery";
        private const string ListenerMnemonic = "seminar cool use bleak drink section rent bid language obey skin round";
        private const string DataPath = @"..\..\..\..\Blockcore.IntegrationTests.Common\ReadyData";

        public GenerateChain()
        {
            Directory.CreateDirectory(DataPath);
        }

        [Fact(Skip = SkipTestMessage)]
        public void CreateStratisBlockchainDataWith10Blocks()
        {
            this.GenerateStratisBlockchainData(new StratisRegTest(), 10, true, true, true);
        }

        [Fact(Skip = SkipTestMessage)]
        public void CreateStratisBlockchainDataWith100Blocks()
        {
            this.GenerateStratisBlockchainData(new StratisRegTest(), 100, true, true, true);
        }

        [Fact(Skip = SkipTestMessage)]
        public void CreateStratisBlockchainDataWith150Blocks()
        {
            this.GenerateStratisBlockchainData(new StratisRegTest(), 150, true, true, true);
        }

        [Fact(Skip = SkipTestMessage)]
        public void CreateBitcoinBlockchainDataWith10Blocks()
        {
            this.GenerateBitcoinBlockchainData(new BitcoinRegTest(), 10, true, true, true);
        }

        [Fact(Skip = SkipTestMessage)]
        public void CreateBitcoinBlockchainDataWith100Blocks()
        {
            this.GenerateBitcoinBlockchainData(new BitcoinRegTest(), 100, true, true, true);
        }

        [Fact(Skip = SkipTestMessage)]
        public void CreateBitcoinBlockchainDataWith150Blocks()
        {
            this.GenerateBitcoinBlockchainData(new BitcoinRegTest(), 150, true, true, true);
        }

        private void GenerateStratisBlockchainData(Network network, int blockCount, bool saveMinerFolderWithWallet, bool saveListenerFolderWithSyncedEmptyWallet, bool saveFolderWithoutWallet)
        {
            string dataFolderPath, listenerFolderPath;

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                // Arrange.
                // Create a sending and a receiving node.
                CoreNode miningNode = builder.CreateStratisPosNode(network).WithWallet(walletMnemonic: MinerMnemonic).Start();
                CoreNode listeningNode = builder.CreateStratisPosNode(network).WithWallet(walletMnemonic: ListenerMnemonic).Start();

                TestHelper.MineBlocks(miningNode, blockCount);
                TestHelper.Connect(miningNode, listeningNode);
                TestHelper.WaitForNodeToSync(miningNode, listeningNode);
                TestBase.WaitLoop(() => miningNode.FullNode.WalletManager().WalletTipHeight == blockCount);
                TestBase.WaitLoop(() => listeningNode.FullNode.WalletManager().WalletTipHeight == blockCount);
                TestBase.WaitLoop(() => miningNode.FullNode.ChainBehaviorState.BlockStoreTip.Height == blockCount);
                TestBase.WaitLoop(() => listeningNode.FullNode.ChainBehaviorState.BlockStoreTip.Height == blockCount);

                TestBase.WaitLoop(() =>
                {
                    return miningNode.FullNode.WalletManager().Wallets.All(a => a.AccountsRoot.All(b => b.LastBlockSyncedHeight == blockCount))
                     && listeningNode.FullNode.WalletManager().Wallets.All(a => a.AccountsRoot.All(b => b.LastBlockSyncedHeight == blockCount));
                });

                dataFolderPath = miningNode.DataFolder;
                listenerFolderPath = listeningNode.DataFolder;
            }

            if (saveMinerFolderWithWallet)
            {
                File.Delete(Path.Combine(dataFolderPath, "stratis.conf"));
                ZipDataFolder(dataFolderPath, $"{network.Name}{blockCount}Miner.zip", DataPath);
            }

            if (saveListenerFolderWithSyncedEmptyWallet)
            {
                File.Delete(Path.Combine(listenerFolderPath, "stratis.conf"));
                ZipDataFolder(listenerFolderPath, $"{network.Name}{blockCount}Listener.zip", DataPath);
            }

            if (saveFolderWithoutWallet)
            {
                foreach (string walletFile in Directory.EnumerateFiles(dataFolderPath, "*.wallet.json", SearchOption.AllDirectories))
                {
                    File.Delete(walletFile);
                }

                ZipDataFolder(dataFolderPath, $"{network.Name}{blockCount}NoWallet.zip", DataPath);
            }
        }

        private void GenerateBitcoinBlockchainData(Network network, int blockCount, bool saveMinerFolderWithWallet, bool saveListenerFolderWithSyncedEmptyWallet, bool saveFolderWithoutWallet)
        {
            string dataFolderPath, listenerFolderPath;

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                // Arrange.
                // Create a sending and a receiving node.
                CoreNode miningNode = builder.CreateStratisPowNode(network).WithWallet(walletMnemonic: MinerMnemonic).Start();
                CoreNode listeningNode = builder.CreateStratisPowNode(network).WithWallet(walletMnemonic: ListenerMnemonic).Start();

                TestHelper.MineBlocks(miningNode, blockCount);
                TestHelper.Connect(miningNode, listeningNode);
                TestHelper.WaitForNodeToSync(miningNode, listeningNode);
                TestBase.WaitLoop(() => miningNode.FullNode.WalletManager().WalletTipHeight == blockCount);
                TestBase.WaitLoop(() => listeningNode.FullNode.WalletManager().WalletTipHeight == blockCount);
                TestBase.WaitLoop(() => miningNode.FullNode.ChainBehaviorState.BlockStoreTip.Height == blockCount);
                TestBase.WaitLoop(() => listeningNode.FullNode.ChainBehaviorState.BlockStoreTip.Height == blockCount);

                TestBase.WaitLoop(() =>
                {
                    return miningNode.FullNode.WalletManager().Wallets.All(a => a.AccountsRoot.All(b => b.LastBlockSyncedHeight == blockCount))
                     && listeningNode.FullNode.WalletManager().Wallets.All(a => a.AccountsRoot.All(b => b.LastBlockSyncedHeight == blockCount));
                });

                dataFolderPath = miningNode.DataFolder;
                listenerFolderPath = listeningNode.DataFolder;
            }

            if (saveMinerFolderWithWallet)
            {
                File.Delete(Path.Combine(dataFolderPath, "bitcoin.conf"));
                ZipDataFolder(dataFolderPath, $"{network.Name}{blockCount}Miner.zip", DataPath);
            }

            if (saveListenerFolderWithSyncedEmptyWallet)
            {
                File.Delete(Path.Combine(listenerFolderPath, "bitcoin.conf"));
                ZipDataFolder(listenerFolderPath, $"{network.Name}{blockCount}Listener.zip", DataPath);
            }

            if (saveFolderWithoutWallet)
            {
                foreach (string walletFile in Directory.EnumerateFiles(dataFolderPath, "*.wallet.json", SearchOption.AllDirectories))
                {
                    File.Delete(walletFile);
                }

                ZipDataFolder(dataFolderPath, $"{network.Name}{blockCount}NoWallet.zip", DataPath);
            }
        }

        private static void ZipDataFolder(string folderToZip, string zipFileName, string destinationFolder)
        {
            string zipPath = Path.Combine(destinationFolder, zipFileName);

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(Path.GetFullPath(folderToZip), zipPath);
        }
    }
}