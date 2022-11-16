using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using Blockcore.Consensus.ScriptInfo;
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
using NBitcoin.DataEncoders;

namespace Blockcore.Features.Miner.Api.Controllers
{
    /// <summary>
    /// Controller providing operations on mining feature.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class StakingController : Microsoft.AspNetCore.Mvc.Controller
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
            this.logger = loggerFactory.CreateLogger(GetType().FullName);
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
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method only available for Proof of Stake");

                GetStakingInfoModel model = this.posMinting != null ? this.posMinting.GetGetStakingInfoModel() : new GetStakingInfoModel();

                return Json(model);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Get staking info from the miner.
        /// </summary>
        /// <returns>All staking info details as per the GetStakingInfoModel.</returns>
        [Route("getnetworkstakinginfo")]
        [HttpGet]
        public IActionResult GetNetworkStakingInfo()
        {
            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method only available for Proof of Stake");

                double networkWeight = this.posMinting.GetNetworkWeight();
                double posDifficulty = this.posMinting.GetDifficulty(null);

                return Json(new GetNetworkStakingInfoModel { Difficulty = posDifficulty, NetStakeWeight = (long)networkWeight });
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
        public IActionResult StartStaking([FromBody] StartStakingRequest request)
        {
            Guard.NotNull(request, nameof(request));

            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method only available for Proof of Stake");

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

                return Ok();
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
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method only available for Proof of Stake");

                this.fullNode.NodeFeature<MiningFeature>(true).StopStaking();
                return Ok();
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
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method only available for Proof of Stake");

                if (!this.minerSettings.EnforceStakingFlag)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Forbidden, "Operation not allowed", "This operation is only allowed if EnforceStakingFlag is true");

                Script redeemScript = null;
                if (!string.IsNullOrEmpty(request.RedeemScript))
                {
                    redeemScript = Script.FromBytesUnsafe(Encoders.Hex.DecodeData(request.RedeemScript));
                }

                Wallet.Types.Wallet wallet = this.walletManager.GetWallet(request.WalletName);

                foreach (HdAccount account in wallet.GetAccounts(account => true))
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        if (address.Address == request.Address)
                        {
                            address.StakingExpiry = request.StakingExpiry;
                        }

                        if (redeemScript != null && address.RedeemScripts != null)
                        {

                            if (address.RedeemScripts.Contains(redeemScript))
                            {
                                if (address.RedeemScriptExpiry == null)
                                    address.RedeemScriptExpiry = new List<RedeemScriptExpiry>();

                                var expiryScript = address.RedeemScriptExpiry.FirstOrDefault(w => w.RedeemScript == redeemScript);

                                if (expiryScript == null)
                                {
                                    expiryScript = new RedeemScriptExpiry
                                    {
                                        RedeemScript = redeemScript,
                                    };

                                    address.RedeemScriptExpiry.Add(expiryScript);
                                }

                                expiryScript.StakingExpiry = request.StakingExpiry;
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        [Route("getStakingNotExpired")]
        [HttpPost]
        public IActionResult GetStakingNotExpired([FromBody] StakingNotExpiredRequest request)
        {
            try
            {
                if (!this.fullNode.Network.Consensus.IsProofOfStake)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.MethodNotAllowed, "Method not allowed", "Method only available for Proof of Stake");

                if (!this.minerSettings.EnforceStakingFlag)
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.Forbidden, "Operation not allowed", "This operation is only allowed if EnforceStakingFlag is true");

                Wallet.Types.Wallet wallet = this.walletManager.GetWallet(request.WalletName);

                GetStakingAddressesModel model = new GetStakingAddressesModel { Addresses = new List<GetStakingAddressesModelItem>() };

                foreach (HdAccount account in wallet.GetAccounts(account => true))
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        GetStakingAddressesModelItem addressItem = null;

                        if (address.StakingExpiry != null)
                        {
                            addressItem = new GetStakingAddressesModelItem
                            {
                                Addresses = address.Address,
                                Expiry = address.StakingExpiry,
                                Expired = address.StakingExpiry < DateTime.UtcNow,
                            };

                            model.Addresses.Add(addressItem);
                        }

                        if (address.RedeemScriptExpiry != null)
                        {
                            foreach (RedeemScriptExpiry redeemScriptExpiry in address.RedeemScriptExpiry)
                            {
                                if (addressItem == null)
                                {
                                    addressItem = new GetStakingAddressesModelItem
                                    {
                                        Addresses = address.Address,
                                        Expiry = address.StakingExpiry,
                                        Expired = address.StakingExpiry < DateTime.UtcNow,
                                    };
                                }

                                var redeemScriptExpiryItem = new RedeemScriptExpiryItem
                                {
                                    RedeemScript = Encoders.Hex.EncodeData(redeemScriptExpiry.RedeemScript.ToBytes(false)),
                                    StakingExpiry = redeemScriptExpiry.StakingExpiry,
                                    Expired = redeemScriptExpiry.StakingExpiry > DateTime.UtcNow
                                };

                                addressItem.RedeemScriptExpiry.Add(redeemScriptExpiryItem);
                            }
                        }
                    }
                }

                return Json(model);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}