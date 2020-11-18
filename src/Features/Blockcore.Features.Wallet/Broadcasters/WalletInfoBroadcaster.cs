using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blockcore.AsyncWork;
using Blockcore.Broadcasters;
using Blockcore.Connection;
using Blockcore.Consensus.Chain;
using Blockcore.EventBus;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Api.Models;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Features.Wallet.Types;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Wallet.Broadcasters
{
    /// <summary>
    /// Broadcasts current staking information to Web Socket clients
    /// </summary>
    public class WalletInfoBroadcaster : ClientBroadcasterBase
    {
        private readonly IWalletManager walletManager;
        private readonly IConnectionManager connectionManager;
        private readonly ChainIndexer chainIndexer;

        public WalletInfoBroadcaster(
            ILoggerFactory loggerFactory,
            IWalletManager walletManager,
            IConnectionManager connectionManager,
            IAsyncProvider asyncProvider,
            INodeLifetime nodeLifetime,
            ChainIndexer chainIndexer,
            IEventsSubscriptionService subscriptionService = null)
            : base(loggerFactory, nodeLifetime, asyncProvider, subscriptionService)
        {
            this.walletManager = walletManager;
            this.connectionManager = connectionManager;
            this.chainIndexer = chainIndexer;
        }

        protected override IEnumerable<EventBase> GetMessages()
        {
            foreach (string walletName in this.walletManager.GetWalletsNames())
            {
                WalletGeneralInfoClientEvent clientEvent = null;
                try
                {
                    Wallet.Types.Wallet wallet = this.walletManager.GetWallet(walletName);
                    IEnumerable<AccountBalance> balances = this.walletManager.GetBalances(walletName, calculatSpendable: true);
                    IList<AccountBalanceModel> accountBalanceModels = new List<AccountBalanceModel>();
                    foreach (var balance in balances)
                    {
                        HdAccount account = balance.Account;

                        var accountBalanceModel = new AccountBalanceModel
                        {
                            CoinType = wallet.Network.Consensus.CoinType,
                            Name = account.Name,
                            HdPath = account.HdPath,
                            AmountConfirmed = balance.AmountConfirmed,
                            AmountUnconfirmed = balance.AmountUnconfirmed,
                            SpendableAmount = balance.SpendableAmount,
                            Addresses = account.GetCombinedAddresses().Select(address =>
                            {
                                (Money confirmedAmount, Money unConfirmedAmount, bool anytrx) = address.GetBalances(wallet.walletStore, account.IsNormalAccount());
                                return new AddressModel
                                {
                                    Address = address.Address,
                                    IsUsed = anytrx,
                                    IsChange = address.IsChangeAddress(),
                                    AmountConfirmed = confirmedAmount,
                                    AmountUnconfirmed = unConfirmedAmount
                                };
                            })
                        };

                        accountBalanceModels.Add(accountBalanceModel);
                    }

                    clientEvent = new WalletGeneralInfoClientEvent
                    {
                        WalletName = walletName,
                        WalletInfo = new WalletGeneralInfoModel
                        {
                            Network = wallet.Network,
                            CreationTime = wallet.CreationTime,
                            LastBlockSyncedHeight = wallet.AccountsRoot.Single().LastBlockSyncedHeight,
                            ConnectedNodes = this.connectionManager.ConnectedPeers.Count(),
                            ChainTip = this.chainIndexer.Tip.Height,
                            IsChainSynced = this.chainIndexer.IsDownloaded(),
                            IsDecrypted = true,
                        },
                        AccountsBalances = accountBalanceModels
                    };

                    // Get the wallet's file path.
                    (string folder, IEnumerable<string> fileNameCollection) = this.walletManager.GetWalletsFiles();
                    string searchFile =
                        Path.ChangeExtension(walletName, this.walletManager.GetWalletFileExtension());
                    string fileName = fileNameCollection.FirstOrDefault(i => i.Equals(searchFile));
                    if (!string.IsNullOrEmpty(folder) && !string.IsNullOrEmpty(fileName))
                    {
                        clientEvent.WalletInfo.WalletFilePath = Path.Combine(folder, fileName);
                    }
                }
                catch (Exception e)
                {
                    this.log.LogError(e, "Exception occurred: {0}");
                }

                if (null != clientEvent)
                {
                    yield return clientEvent;
                }
            }
        }
    }
}