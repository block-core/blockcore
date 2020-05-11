using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration.Logging;
using Blockcore.Features.SignalR.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Blockcore.Base.Deployments;

namespace Blockcore.Features.SignalR.Hubs
{
    public class NodeHub : Hub
    {
        private readonly ILogger<NodeHub> logger;
        private readonly CommandDispatcher commandDispatcher;

        public NodeHub(ILoggerFactory loggerFactory, CommandDispatcher commandDispatcher)
        {
            this.logger = loggerFactory.CreateLogger<NodeHub>();
            this.commandDispatcher = commandDispatcher;
        }

        public Task Command(string type, string command, object[]? args)
        {
            var result = this.commandDispatcher.Execute(type, command, args);
            return this.Clients.Caller.SendAsync("Command", result);
        }
    }
}
