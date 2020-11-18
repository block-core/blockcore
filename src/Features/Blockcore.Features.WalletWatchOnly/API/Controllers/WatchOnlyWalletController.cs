using System;
using System.Collections.Generic;
using System.Net;
using Blockcore.Consensus.TransactionInfo;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Blockcore.Controllers.Models;
using Blockcore.Features.WalletWatchOnly.Models;
using Blockcore.Features.WalletWatchOnly.Interfaces;
using Blockcore.Utilities.JsonErrors;
using Microsoft.AspNetCore.Authorization;

namespace Blockcore.Features.WalletWatchOnly.Api.Controllers
{
    /// <summary>
    /// Controller providing operations on a watch-only wallet.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class WatchOnlyWalletController : Controller
    {
        /// <summary> The watch-only wallet manager. </summary>
        private readonly IWatchOnlyWalletManager watchOnlyWalletManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOnlyWalletController"/> class.
        /// </summary>
        /// <param name="watchOnlyWalletManager">The watch-only wallet manager.</param>
        public WatchOnlyWalletController(IWatchOnlyWalletManager watchOnlyWalletManager)
        {
            this.watchOnlyWalletManager = watchOnlyWalletManager;
        }

        /// <summary>
        /// Adds a base58 address to the watch list.
        /// </summary>
        /// <example>Request URL: /api/watchonlywallet/watch?address=mpK6g... </example>
        /// <param name="address">The base58 address to add to the watch list.</param>
        [Route("watch")]
        [HttpPost]
        public IActionResult Watch([FromBody]string address)
        {
            // Checks the request is valid.
            if (string.IsNullOrEmpty(address))
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", "Address to watch is missing.");
            }

            try
            {
                this.watchOnlyWalletManager.WatchAddress(address);
                return this.Ok();
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Conflict, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets the list of addresses being watched along with the transactions affecting them.
        /// </summary>
        /// <example>Request URL: /api/watchonlywallet </example>
        /// <returns>The watch-only wallet or a collection of errors, if any.</returns>
        [HttpGet]
        public IActionResult GetWatchOnlyWallet()
        {
            try
            {
                // Map a watch-only wallet to a model object for display in the front end.
                WatchOnlyWallet watchOnlyWallet = this.watchOnlyWalletManager.GetWatchOnlyWallet();
                var model = new WatchOnlyWalletModel
                {
                    CoinType = watchOnlyWallet.CoinType,
                    Network = watchOnlyWallet.Network,
                    CreationTime = watchOnlyWallet.CreationTime
                };

                foreach (KeyValuePair<string, WatchedAddress> watchAddress in watchOnlyWallet.WatchedAddresses)
                {
                    var watchedAddressModel = new WatchedAddressModel
                    {
                        Address = watchAddress.Value.Address,
                        Transactions = new List<TransactionVerboseModel>()
                    };

                    foreach (KeyValuePair<string, WatchTransactionData> transactionData in watchAddress.Value.Transactions)
                    {
                        Transaction transaction = watchOnlyWallet.Network.CreateTransaction(transactionData.Value.Hex);
                        watchedAddressModel.Transactions.Add(new TransactionVerboseModel(transaction, watchOnlyWallet.Network));
                    }

                    model.WatchedAddresses.Add(watchedAddressModel);
                }

                foreach (KeyValuePair<string, WatchTransactionData> transaction in watchOnlyWallet.WatchedTransactions)
                {
                    var watchedTransactionModel = new WatchedTransactionModel
                    {
                        Transaction = new TransactionVerboseModel(watchOnlyWallet.Network.CreateTransaction(transaction.Value.Hex), watchOnlyWallet.Network)
                    };

                    model.WatchedTransactions.Add(watchedTransactionModel);
                }

                return this.Json(model);
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}