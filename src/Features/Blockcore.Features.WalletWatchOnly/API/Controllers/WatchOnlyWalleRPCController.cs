using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Controllers;
using Blockcore.Controllers.Models;
using Blockcore.Features.RPC;
using Blockcore.Features.RPC.Exceptions;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Features.Wallet.Types;
using Blockcore.Features.WalletWatchOnly.Api.Models;
using Blockcore.Features.WalletWatchOnly.Interfaces;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace Blockcore.Features.WalletWatchOnly.Api.Controllers
{
    /// <summary>
    /// Controller providing RPC operations on a watch-only wallet.
    /// </summary>
    public class WatchOnlyWalleRPCController : FeatureController
    {
        /// <summary>Full Node.</summary>
        private readonly IFullNode fullNode;

        /// <summary>Thread safe access to the best chain of block headers from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>Specification of the network the node runs on.</summary>
        private readonly Network network;

        /// <summary>Wallet related configuration.</summary>
        private readonly WalletSettings walletSettings;

        /// <summary>Wallet manager.</summary>
        private readonly IWalletManager walletManager;

        /// <summary>Wallet manager.</summary>
        private readonly IWatchOnlyWalletManager watchOnlyWalletManager;

        /// <summary>Provides access to the block store database.</summary>
        private readonly IBlockStore blockStore;

        /// <summary>Information about node's chain.</summary>
        private readonly IChainState chainState;

        /// <summary>
        /// The wallet name set by selectwallet method. This is static since the controller is a stateless type. This value should probably be cached by an injected service in the future.
        /// </summary>
#pragma warning disable 649
        private static readonly string CurrentWalletName;
#pragma warning restore 649

        /// <inheritdoc />
        public WatchOnlyWalleRPCController(
            IFullNode fullNode,
            IConsensusManager consensusManager,
            ChainIndexer chainIndexer,
            Network network,
            WalletSettings walletSettings,
            IWalletManager walletManager,
            IWatchOnlyWalletManager watchOnlyWalletManager,
            IBlockStore blockStore,
            IChainState chainState) : base(fullNode: fullNode, consensusManager: consensusManager, chainIndexer: chainIndexer, network: network)
        {
            this.fullNode = fullNode;
            this.chainIndexer = chainIndexer;
            this.network = network;
            this.walletSettings = walletSettings;
            this.walletManager = walletManager;
            this.watchOnlyWalletManager = watchOnlyWalletManager;
            this.blockStore = blockStore;
            this.chainState = chainState;
        }

        [ActionName("listtransactions")]
        [ActionDescription("Returns up to 'count' most recent transactions skipping the first 'skip' transactions.")]
        public ListTransactionsModel[] ListTransactions(string account = "*", int count = 10, int skip = 0, bool include_watchonly = true)
        {
            List<ListTransactionsModel> result = new List<ListTransactionsModel>();
            WalletAccountReference accountReference = this.GetWalletAccountReference();

            if (include_watchonly)
            {
                var selectedWatchOnlyTransactions = this.watchOnlyWalletManager.GetWatchedTransactions().Values
                    .Skip(skip)
                    .Take(count);
                foreach (var transactionData in selectedWatchOnlyTransactions)
                {
                    var transactionInfo = this.GetTransactionInfo(transactionData.Id);
                    var transactionResult = this.GetTransactionsModel(transactionInfo);
                    result.Add(transactionResult);
                }
            }

            Wallet.Types.Wallet wallet = this.walletManager.GetWallet(accountReference.WalletName);
            Func<HdAccount, bool> accountFilter = null;
            if (account == "*" || account == null)
            {
                accountFilter = Wallet.Types.Wallet.AllAccounts;
            }
            else
            {
                accountFilter = a => a.Name == account;
            }
            IEnumerable<TransactionOutputData> selectedTransactions = wallet.GetAllTransactions(accountFilter)
                .Skip(skip)
                .Take(count);
            foreach (var transactionData in selectedTransactions)
            {
                var transactionInfo = this.GetTransactionInfo(transactionData.Id);
                var transactionResult = this.GetTransactionsModel(transactionInfo);
                result.Add(transactionResult);
            }

            return result.ToArray();
        }

        internal ListTransactionsModel GetTransactionsModel(TransactionVerboseModel transactionInfo)
        {
            var transactionResult = new ListTransactionsModel
            {
                Confirmations = transactionInfo.Confirmations ?? 0,
                BlockHash = transactionInfo.BlockHash ?? string.Empty,
                BlockTime = transactionInfo.BlockTime ?? 0,
                TransactionId = transactionInfo.TxId,
                TransactionTime = (long)(transactionInfo.Time ?? 0),
                Amount = transactionInfo.VOut.Sum(a => a.Value)
            };
            return transactionResult;
        }

        internal TransactionVerboseModel GetTransactionInfo(uint256 transactionId)
        {
            Transaction transaction = this.blockStore?.GetTransactionById(transactionId);
            ChainedHeader block = this.GetTransactionBlock(transactionId, this.fullNode, this.chainIndexer);
            var transactionInfo = new TransactionVerboseModel(transaction, this.network, block, this.chainState?.ConsensusTip);

            return transactionInfo;
        }

        internal ChainedHeader GetTransactionBlock(uint256 trxid, IFullNode fullNode, ChainIndexer chain)
        {
            Guard.NotNull(fullNode, nameof(fullNode));

            ChainedHeader block = null;
            uint256 blockid = this.blockStore?.GetBlockIdByTransactionId(trxid);
            if (blockid != null)
            {
                block = chain?.GetHeader(blockid);
            }
            return block;
        }

        /// <summary>
        /// Gets the first account from the "default" wallet if it specified,
        /// otherwise returns the first available account in the existing wallets.
        /// </summary>
        /// <returns>Reference to the default wallet account, or the first available if no default wallet is specified.</returns>
        private WalletAccountReference GetWalletAccountReference()
        {
            string walletName = null;

            // If the global override is null or empty.
            if (string.IsNullOrWhiteSpace(CurrentWalletName))
            {
                if (this.walletSettings.IsDefaultWalletEnabled())
                    walletName = this.walletManager.GetWalletsNames().FirstOrDefault(w => w == this.walletSettings.DefaultWalletName);
                else
                {
                    //TODO: Support multi wallet like core by mapping passed RPC credentials to a wallet/account
                    walletName = this.walletManager.GetWalletsNames().FirstOrDefault();
                }
            }
            else
            {
                // Read from class instance the wallet name.
                walletName = CurrentWalletName;
            }

            if (walletName == null)
            {
                throw new RPCServerException(RPCErrorCode.RPC_INVALID_REQUEST, "No wallet found");
            }

            HdAccount account = this.walletManager.GetAccounts(walletName).First();
            return new WalletAccountReference(walletName, account.Name);
        }
    }
}