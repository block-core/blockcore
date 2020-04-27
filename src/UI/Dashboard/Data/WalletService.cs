using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Features.Wallet.Interfaces;
using NBitcoin;

namespace Dashboard.Data
{
    public class WalletService
    {
        public WalletService(Network network, IWalletManager walletManager)
        {
            this.Network = network;
            this.WalletManager = walletManager;
        }

        public Network Network { get; }
        public IWalletManager WalletManager { get; }
    }
}