using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Features.Wallet.Interfaces;
using NBitcoin;

namespace Dashboard.Data
{
    public class WalletService
    {
        public WalletService(Network network, IWalletManager walletManager, IWalletTransactionHandler walletTransactionHandler, IBroadcasterManager broadcasterManager, IWalletSyncManager walletSyncManager)
        {
            this.Network = network;
            this.WalletManager = walletManager;
            this.WalletTransactionHandler = walletTransactionHandler;
            this.BroadcasterManager = broadcasterManager;
            this.WalletSyncManager = walletSyncManager;
        }

        public Network Network { get; }
        public IWalletManager WalletManager { get; }
        public IWalletTransactionHandler WalletTransactionHandler { get; }
        public IBroadcasterManager BroadcasterManager { get; }
        public IWalletSyncManager WalletSyncManager { get; }
    }
}