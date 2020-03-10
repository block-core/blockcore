using System;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using Blockcore;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.Api;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.ColdStaking;
using Blockcore.Features.SignalR;
using Blockcore.Features.SignalR.Broadcasters;
using Blockcore.Features.SignalR.Events;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Features.Diagnostic;

namespace Blockcore.Stratis
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Networks.Stratis,
                    protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                {
                    MinProtocolVersion = ProtocolVersion.ALT_PROTOCOL_VERSION
                };

                IFullNodeBuilder nodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseColdStakingWallet()
                    .AddPowPosMining()
                    .UseApi()
                    .AddRPC()
                    .UseDiagnosticFeature();

                if (nodeSettings.EnableSignalR)
                {
                    nodeBuilder.AddSignalR(options =>
                    {
                        options.EventsToHandle = new[]
                        {
                            (IClientEvent) new BlockConnectedClientEvent(),
                            new TransactionReceivedClientEvent()
                        };

                        options.ClientEventBroadcasters = new[]
                        {
                            (Broadcaster: typeof(StakingBroadcaster), ClientEventBroadcasterSettings: new ClientEventBroadcasterSettings
                                {
                                    BroadcastFrequencySeconds = 5
                                }),
                            (Broadcaster: typeof(WalletInfoBroadcaster), ClientEventBroadcasterSettings: new ClientEventBroadcasterSettings
                                {
                                    BroadcastFrequencySeconds = 5
                                })
                        };
                    });
                }

                IFullNode node = nodeBuilder.Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }
    }
}