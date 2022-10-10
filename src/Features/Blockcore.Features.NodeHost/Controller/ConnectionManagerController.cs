using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Blockcore.Connection;
using Blockcore.P2P.Peer;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Blockcore.Utilities.JsonErrors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blockcore.Controllers
{
    /// <summary>
    /// A <see cref="FeatureController"/> that implements API and RPC methods for the connection manager.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class ConnectionManagerController : FeatureController
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        private readonly IPeerBanning peerBanning;

        public ConnectionManagerController(IConnectionManager connectionManager,
            ILoggerFactory loggerFactory, IPeerBanning peerBanning) : base(connectionManager: connectionManager)
        {
            Guard.NotNull(this.ConnectionManager, nameof(this.ConnectionManager));
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.peerBanning = peerBanning;
        }

        /// <summary>
        /// Sends a command to the connection manager.
        /// </summary>
        /// <param name="endpoint">The endpoint in string format. Specify an IP address. The default port for the network will be added automatically.</param>
        /// <param name="command">The command to run. {add, remove, onetry}</param>
        /// <returns>Json formatted <c>True</c> indicating success. Returns <see cref="IActionResult"/> formatted exception if fails.</returns>
        /// <remarks>This is an API implementation of an RPC call.</remarks>
        /// <exception cref="ArgumentException">Thrown if either command not supported/empty or if endpoint is invalid/empty.</exception>
        [Route("addnode")]
        [HttpGet]
        public IActionResult AddNode([FromQuery] string endpoint, string command)
        {
            try
            {
                Guard.NotEmpty(endpoint, nameof(endpoint));
                Guard.NotEmpty(command, nameof(command));

                return this.Json(ConnectionManagerHelper.AddNode(this.ConnectionManager, this.peerBanning, endpoint, command));
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets information about this node.
        /// </summary>
        /// <see cref="https://github.com/bitcoin/bitcoin/blob/0.14/src/rpc/net.cpp"/>
        /// <remarks>This is an API implementation of an RPC call.</remarks>
        /// <returns>Json formatted <see cref="List{T}<see cref="PeerNodeModel"/>"/> of connected nodes. Returns <see cref="IActionResult"/> formatted error if fails.</returns>
        [AllowAnonymous]
        [Route("getpeerinfo")]
        [HttpGet]
        public IActionResult GetPeerInfo()
        {
            try
            {
                return this.Json(ConnectionManagerHelper.GetPeerInfo(this.ConnectionManager));
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}