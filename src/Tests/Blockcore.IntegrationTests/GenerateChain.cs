using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.Networks;
using Blockcore.Networks.Bitcoin;
using Blockcore.Networks.Stratis;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonConverters;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Xunit;

namespace Blockcore.IntegrationTests
{
    public class GenerateChain
    {
        private const string SkipTestMessage = "Not to be run as part of the unit test suite.";

        private const string MinerMnemonic = "elevator slight dad hair table forum maze feed trim ignore field mystery";
        private const string ListenerMnemonic = "seminar cool use bleak drink section rent bid language obey skin round";
        private const string DataPath = @"..\..\..\..\Blockcore.IntegrationTests.Common\ReadyData";
        private const string WalletOutputDataPath = @"..\..\..\..\\Blockcore.IntegrationTests\Wallet\Data";

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

        [Fact(Skip = SkipTestMessage)]
        public void CreateWalletData()
        {
            string walltName = "wallet-with-funds";
            string path = Path.Combine(WalletOutputDataPath + @"\txdb", walltName + ".db");

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var network = new BitcoinRegTest();
            var folder = new DataFolder(WalletOutputDataPath);
            var wallet = new Features.Wallet.Types.Wallet() { Name = walltName };

            var walletStore = new WalletStore(network, folder, wallet);

            string dataPath = Path.Combine(DataPath, "wallet-data-table.json");

            var dataTable = JsonConvert.DeserializeObject<WalletDataItem[]>(File.ReadAllText(dataPath))[0];

            WalletData walletData = new WalletData
            {
                Key = dataTable.Id,
                WalletName = dataTable.WalletName,
                EncryptedSeed = dataTable.EncryptedSeed,
                WalletTip = new HashHeightPair(uint256.Parse(dataTable.WalletTip.Split("-")[1]),
                    int.Parse(dataTable.WalletTip.Split("-")[0])),
                BlockLocator = dataTable.BlockLocator.Select(uint256.Parse).ToList()
            };
            walletStore.SetData(walletData);

            dataPath = Path.Combine(DataPath, "wallet-transactions-table.json");

            var transactionsTable = JsonConvert.DeserializeObject<TransactionDataItem[]>(File.ReadAllText(dataPath));
            foreach (var item in transactionsTable)
            {
                var trx = new TransactionOutputData
                {
                    OutPoint = new OutPoint(uint256.Parse(item.OutPoint.Split("-")[0]),
                        int.Parse(item.OutPoint.Split("-")[1])),
                    Address = item.Address,
                    Id = uint256.Parse(item.Id),
                    Amount = new Money(item.Amount),
                    Index = item.Index,
                    BlockHeight = item.BlockHeight,
                    BlockHash = item.BlockHash != null ? uint256.Parse(item.BlockHash) : null,
                    CreationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item.CreationTime)),
                    ScriptPubKey = new Script(Encoders.Hex.DecodeData(item.ScriptPubKey)),
                    IsPropagated = item.IsPropagated,
                    AccountIndex = item.AccountIndex,
                    IsCoinStake = item.IsCoinStake,
                    IsCoinBase = item.IsCoinBase,
                    IsColdCoinStake = item.IsColdCoinStake,
                };

                if (item.SpendingDetails != null)
                {
                    trx.SpendingDetails = new Features.Wallet.Database.SpendingDetails
                    {
                        BlockHeight = item.SpendingDetails.BlockHeight,
                        IsCoinStake = item.SpendingDetails.IsCoinStake,
                        CreationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item.SpendingDetails.CreationTime)),
                        TransactionId = uint256.Parse(item.SpendingDetails.TransactionId)
                    };

                    if (item.SpendingDetails.Payments != null)
                    {
                        foreach (PaymentDetails spendingDetailsPayment in item.SpendingDetails.Payments)
                        {
                            trx.SpendingDetails.Payments.Add(new Features.Wallet.Database.PaymentDetails
                            {
                                Amount = new Money(spendingDetailsPayment.Amount),
                                DestinationAddress = spendingDetailsPayment.DestinationAddress,
                                DestinationScriptPubKey = new Script(Encoders.Hex.DecodeData(spendingDetailsPayment.DestinationScriptPubKey)),
                                OutputIndex = spendingDetailsPayment.OutputIndex,
                                PayToSelf = spendingDetailsPayment.PayToSelf
                            });
                        }
                    }
                }

                walletStore.InsertOrUpdate(trx);
            }
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

    public class WalletDataItem
    {
        public string Id { get; set; }

        public string EncryptedSeed { get; set; }

        public string WalletName { get; set; }

        public string WalletTip { get; set; }

        public ICollection<string> BlockLocator { get; set; }
    }

    public class TransactionDataItem
    {
        public string OutPoint { get; set; }

        public string Address { get; set; }

        public int AccountIndex { get; set; }

        public string Id { get; set; }

        public long Amount { get; set; }

        public bool? IsCoinBase { get; set; }

        public bool? IsCoinStake { get; set; }
        public bool? IsColdCoinStake { get; set; }

        public int Index { get; set; }

        public int? BlockHeight { get; set; }

        public string BlockHash { get; set; }

        public int? BlockIndex { get; set; }

        public string CreationTime { get; set; }

        public string ScriptPubKey { get; set; }

        public bool? IsPropagated { get; set; }

        public SpendingDetails SpendingDetails { get; set; }
    }

    public class PaymentDetails
    {
        public string DestinationScriptPubKey { get; set; }

        public string DestinationAddress { get; set; }

        public int? OutputIndex { get; set; }

        public long Amount { get; set; }

        public bool? PayToSelf { get; set; }
    }

    public class SpendingDetails
    {
        public SpendingDetails()
        {
            this.Payments = new List<PaymentDetails>();
        }

        public string TransactionId { get; set; }

        public ICollection<PaymentDetails> Payments { get; set; }

        public int? BlockHeight { get; set; }

        public int? BlockIndex { get; set; }

        public bool? IsCoinStake { get; set; }

        public string CreationTime { get; set; }
    }
}