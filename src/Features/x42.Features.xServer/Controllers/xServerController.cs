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
    public class XServerController : Controller
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// Manager for xServers
        /// </summary>
        private readonly IxServerManager xServerManager;

        public XServerController(ILoggerFactory loggerFactory, IxServerManager xServerManager)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(xServerManager, nameof(IxServerManager));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.xServerManager = xServerManager;
        }

        /// <summary>
        /// Retrieves the xServer stats
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
                    Connected = this.xServerManager.ConnectedSeeds.Count
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
        /// Will broadcast the registration for xServer
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
    }
}