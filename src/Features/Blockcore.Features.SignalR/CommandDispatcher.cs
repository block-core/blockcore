using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
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
using Blockcore.Features.Wallet.Interfaces;
using LevelDB;
using Blockcore.Utilities;

namespace Blockcore.Features.SignalR
{
    public class CommandResult
    {
        public string Service { get; set; }

        public string Command { get; set; }

        public object Result { get; set; }
    }

    public class CommandDispatcher
    {
        private readonly IServiceProvider serviceProvider;

        private readonly Dictionary<string, object> services;

        public CommandDispatcher(IServiceProvider serviceProvider, IWalletManager walletManager, INodeLifetime nodeLifetime)
        {
            // White-listing of services made available through the dispatcher.
            this.services = new Dictionary<string, object>();
            this.services.Add("WalletManager", walletManager);
            this.services.Add("NodeLifetime", nodeLifetime);

            this.serviceProvider = serviceProvider;
        }

        public string Execute(string service, string command, object[]? args)
        {
            var instance = this.services[service];
            var type = instance.GetType();
            var methodInfo = type.GetMethod(command);
            var result = methodInfo.Invoke(instance, args);

            var envelope = new CommandResult();
            envelope.Service = service;
            envelope.Command = command;
            envelope.Result = result;

            return JsonConvert.SerializeObject(envelope);
        }
    }
}
