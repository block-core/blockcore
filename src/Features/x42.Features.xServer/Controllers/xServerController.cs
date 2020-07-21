using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonErrors;
using Blockcore.Utilities.ModelStateErrors;
using x42.Features.xServer.Interfaces;
using x42.Features.xServer.Models;

namespace x42.Features.xServer.Controllers
{
    /// <summary>Controller providing operations for the xServer network.</summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class xServerController : Controller
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Manager for xServers
        /// </summary>
        private readonly IxServerManager xServerManager;

        /// <summary>
        ///     Constructor for the xServer contoller
        /// </summary>
        public xServerController(ILoggerFactory loggerFactory, IxServerManager xServerManager)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(xServerManager, nameof(IxServerManager));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.xServerManager = xServerManager;
        }

        /// <summary>
        ///     Retrieves the xServer stats
        /// </summary>
        /// <returns>Returns the stats of the xServer network.</returns>
        [Route("getxserverstats")]
        [HttpGet]
        public IActionResult GetXServerStats()
        {
            if (!this.ModelState.IsValid)
            {
                return ModelStateErrors.BuildErrorResponse(this.ModelState);
            }

            try
            {
                var serverStats = new GetXServerStatsResult()
                {
                    Connected = this.xServerManager.ConnectedSeeds.Count,
                    Nodes = this.xServerManager.ConnectedSeeds
                };

                return this.Json(serverStats);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Will broadcast the registration for xServer
        /// </summary>
        /// <param name="registerRequest">The object with all of the nessesary data to register a xServer.</param>
        /// <returns>Returns true if the registration was successfully recived, otherwise false with a reason.</returns>
        [Route("registerxserver")]
        [HttpPost]
        public IActionResult RegisterXServer([FromBody] RegisterRequest registerRequest)
        {
            if (!this.ModelState.IsValid)
            {
                return ModelStateErrors.BuildErrorResponse(this.ModelState);
            }
            
            try
            {
                var result = this.xServerManager.RegisterXServer(registerRequest);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Test request for xServer Ports to see if they are available.
        /// </summary>
        /// <param name="testRequest">The object with all of the nessesary data to register a xServer.</param>
        /// <returns>Returns test result if the registration was successfully recived, otherwise false with a reason.</returns>
        [Route("testxserverports")]
        [HttpPost]
        public IActionResult TestXServerPorts([FromBody] TestRequest testRequest)
        {
            if (!this.ModelState.IsValid)
            {
                return ModelStateErrors.BuildErrorResponse(this.ModelState);
            }

            try
            {
                var result = this.xServerManager.TestXServerPorts(testRequest);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Get available price lock pairs
        /// </summary>
        /// <returns>A list with all of the available pairs for a price lock.</returns>
        [HttpGet]
        [Route("getavailablepairs")]
        public IActionResult GetAvailablePairs()
        {
            try
            {
                var result = this.xServerManager.GetAvailablePairs();
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Create a price lock.
        /// </summary>
        /// <param name="priceLockRequest">The object with all of the nessesary data to create a price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock results.</returns>
        [HttpPost]
        [Route("createpricelock")]
        public IActionResult CreatePriceLock([FromBody] CreatePriceLockRequest priceLockRequest)
        {
            try
            {
                var result = this.xServerManager.CreatePriceLock(priceLockRequest);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Get a price lock.
        /// </summary>
        /// <param name="priceLockId">The ID of the price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock information.</returns>
        [HttpGet]
        [Route("getpricelock")]
        public IActionResult GetPriceLock(string priceLockId)
        {
            try
            {
                var result = this.xServerManager.GetPriceLock(priceLockId);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Submit the payment for a price lock.
        /// </summary>
        /// <param name="submitPaymentRequest">The object with all of the nessesary data to submit payment.</param>
        /// <returns>A <see cref="SubmitPaymentResult" /> with submission results.</returns>
        [HttpPost]
        [Route("submitpayment")]
        public IActionResult SubmitPayment([FromBody] SubmitPaymentRequest submitPaymentRequest)
        {
            try
            {
                var result = this.xServerManager.SubmitPayment(submitPaymentRequest);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Will lookup the profile, and return the profile data.
        /// </summary>
        /// <returns>A JSON object containing the profile requested.</returns>
        [HttpGet]
        [Route("getprofile")]
        public IActionResult GetProfile(string name = "", string keyAddress = "")
        {
            try
            {
                var result = this.xServerManager.GetProfile(name, keyAddress);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        ///     Reserves a profile to the network.
        /// </summary>
        /// <param name="reserveRequest">The object with all of the nessesary data to reserve a profile.</param>
        /// <returns>A <see cref="ReserveProfileResult" /> with reservation result.</returns>
        [HttpPost]
        [Route("reserveprofile")]
        public IActionResult ReserveProfile([FromBody] ProfileReserveRequest reserveRequest)
        {
            try
            {
                var result = this.xServerManager.ReserveProfile(reserveRequest);
                return Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}