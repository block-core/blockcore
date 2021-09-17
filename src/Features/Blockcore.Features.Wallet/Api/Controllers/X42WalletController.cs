using System;
using System.Linq;
using System.Net;
using Blockcore.Connection;
using Blockcore.Consensus.Chain;
using Blockcore.Features.Wallet.Api.Models;
using Blockcore.Features.Wallet.Api.Models.X42;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonErrors;
using Blockcore.Utilities.ModelStateErrors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.Wallet.Api.Controllers
{

    /// <summary>
    /// Controller providing operations on a wallet.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class X42WalletController : Controller
    {
        private readonly IWalletManager walletManager;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public X42WalletController(ILoggerFactory loggerFactory, IWalletManager walletManager, Network network)
        {
            this.walletManager = walletManager;
            this.network = network;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }



        /// <summary>
        /// Gets the history of a wallet with reduced metadata. Note: This method will filter out transactions sent to self wallet.
        /// </summary>
        /// <param name="request">An object containing the parameters used to retrieve a wallet's history.</param>
        /// <returns>A JSON object containing the wallet history.</returns>
        [Route("history")]
        [HttpGet]
        public IActionResult GetHistory([FromQuery] x42WalletHistoryRequest request)
        {
            Guard.NotNull(request, nameof(request));

            if (!this.ModelState.IsValid)
            {
                return ModelStateErrors.BuildErrorResponse(this.ModelState);
            }

            try
            {
                var skip = request.Skip;
                var take = request.Take;

                request.Skip = null;
                request.Take = null;

                WalletHistoryModel model = WalletModelBuilder.GetHistory(this.walletManager, this.network, request);

                var transactionHistory = model.AccountsHistoryModel.FirstOrDefault().TransactionsHistory;

                var transactionData = transactionHistory.ToList();
                var count = 0;

                if (request.TransactionType != null)
                {

                    transactionData = transactionData.Where(l => l.Type.ToString() == request.TransactionType).ToList();
                }

                count = transactionData.Count();

                if (skip != null && take != null) {

                    transactionData = transactionData.Skip(skip.Value).Take(take.Value).ToList();
                }

                var res = new PagedResultModel<TransactionItemModel>()
                {

                    TotalCount = count,
                    Data = transactionData

                };

                return this.Json(res);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }


    }
}
