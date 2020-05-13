using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.NodeHost.Hubs
{
    /// <summary>
    /// Node Hub can be used to perform many tasks on the node, including the majority of features available in the REST API.
    /// </summary>
    public class NodeHub : Hub
    {
        private readonly ILogger<NodeHub> logger;

        public NodeHub(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<NodeHub>();
        }

        /// <summary>
        /// Basic echo method that can be used to verify connection.
        /// </summary>
        /// <param name="message">Any message to echo back.</param>
        /// <returns>Returns the same message supplied.</returns>
        public Task Echo(string message)
        {
            return this.Clients.Caller.SendAsync("Echo", message);
        }
    }
}
