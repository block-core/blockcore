﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using Blockcore.Features.Miner.Api.Models;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Features.Wallet.Types;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonErrors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Miner.Api.Controllers
{
    /// <summary>
    /// Controller providing operations on mining feature.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class StakingController : Controller
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>PoS staker.</summary>
        private readonly IPosMinting posMinting;

        /// <summary>Full Node.</summary>
        private readonly IFullNode fullNode;

        /// <summary>The wallet manager.</summary>
        private readonly IWalletManager walletManager;

        private readonly MinerSettings minerSettings;

        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        public StakingController(IFullNode fullNode, ILoggerFactory loggerFactory, IWalletManager walletManager, MinerSettings minerSettings, IPosMinting posMinting = null)
        {
            Guard.NotNull(fullNode, nameof(fullNode));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(walletManager, nameof(walletManager));

            this.fullNode = fullNode;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.walletManager = walletManager;
            this.minerSettings = minerSettings;
            this.posMinting = posMinting;
        }

        /// <summary>
        /// Get staking info from the miner.
        /// </summary>
        /// <returns>All staking info details as per the GetStakingInfoModel.</returns>
        [Route("getstakinginfo")]
        [HttpGet]
        public IActionResult GetStakingInfo()
        {
            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method not available for Proof of Stake");

                GetStakingInfoModel model = this.posMinting != null ? this.posMinting.GetGetStakingInfoModel() : new GetStakingInfoModel();

                return this.Json(model);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Start staking.
        /// </summary>
        /// <param name="request">The name and password of the wallet to stake.</param>
        /// <returns>An <see cref="OkResult"/> object that produces a status code 200 HTTP response.</returns>
        [Route("startstaking")]
        [HttpPost]
        public IActionResult StartStaking([FromBody]StartStakingRequest request)
        {
            Guard.NotNull(request, nameof(request));

            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method not available for Proof of Stake");

                if (!this.ModelState.IsValid)
                {
                    IEnumerable<string> errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
                }

                Wallet.Types.Wallet wallet = this.walletManager.GetWallet(request.Name);

                // Check the password
                try
                {
                    Key.Parse(wallet.EncryptedSeed, request.Password, wallet.Network);
                }
                catch (Exception ex)
                {
                    throw new SecurityException(ex.Message);
                }

                this.fullNode.NodeFeature<MiningFeature>(true).StartStaking(request.Name, request.Password);

                return this.Ok();
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Stop staking.
        /// </summary>
        /// <param name="corsProtection">This body parameter is here to prevent a CORS call from triggering method execution.</param>
        /// <remarks>
        /// <seealso cref="https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS#Simple_requests"/>
        /// </remarks>
        /// <returns>An <see cref="OkResult"/> object that produces a status code 200 HTTP response.</returns>
        [Route("stopstaking")]
        [HttpPost]
        public IActionResult StopStaking([FromBody] bool corsProtection = true)
        {
            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method not available for Proof of Stake");

                this.fullNode.NodeFeature<MiningFeature>(true).StopStaking();
                return this.Ok();
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Set expiration for an address for staking, this only allowed if <see cref="MinerSettings.EnforceStakingFlag"/> is true.
        /// </summary>
        /// <returns>An <see cref="OkResult"/> object that produces a status code 200 HTTP response.</returns>
        [Route("stakingExpiry")]
        [HttpPost]
        public IActionResult StakingExpiry([FromBody] StakingExpiryRequest request)
        {
            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method not available for Proof of Stake");

                if (!this.minerSettings.EnforceStakingFlag)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Forbidden, "Operation not allowed", "This operation is only allowed if EnforceStakingFlag is true");

                Wallet.Types.Wallet wallet = this.walletManager.GetWallet(request.WalletName);

                foreach (HdAccount account in wallet.GetAccounts(account => true))
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        if ((address.Address == request.Address) || address.Bech32Address == request.Address)
                        {
                            address.StakingExpiry = request.StakingExpiry;
                        }
                    }
                }

                return this.Ok();
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        [Route("getStakingNotExpired")]
        [HttpPost]
        public IActionResult GetStakingNotExpired(StakingNotExpiredRequest request)
        {
            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method not available for Proof of Stake");

                if (!this.minerSettings.EnforceStakingFlag)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Forbidden, "Operation not allowed", "This operation is only allowed if EnforceStakingFlag is true");

                Wallet.Types.Wallet wallet = this.walletManager.GetWallet(request.WalletName);

                GetStakingAddressesModel model = new GetStakingAddressesModel { Addresses = new List<GetStakingAddressesModelItem>() };

                foreach (HdAccount account in wallet.GetAccounts(account => true))
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        if (address.StakingExpiry != null && address.StakingExpiry > DateTime.UtcNow)
                        {
                            model.Addresses.Add(new GetStakingAddressesModelItem
                            {
                                Addresses = request.Segwit ? address.Bech32Address : address.Address,
                                Expiry = address.StakingExpiry
                            });
                        }
                    }
                }

                return this.Json(model);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}