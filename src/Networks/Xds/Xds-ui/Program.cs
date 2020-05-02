using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Features.Api;
using Blockcore.Features.BlockStore;
using Blockcore.Features.ColdStaking;
using Blockcore.Features.Consensus;
using Blockcore.Features.Diagnostic;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Networks.Xds;
using Blockcore.Utilities;
using NBitcoin.Protocol;
using WebWindows;

namespace StratisD
{
    public class Program
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

#pragma warning disable IDE1006 // Naming Styles

        public static async Task Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            try
            {
                var nodeSettings = new NodeSettings(networksSelector: Networks.Xds,
                    protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION,
                    args: args);

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

                var node = nodeBuilder.Build();
                node.RunAsync();

                HideConsole();

                var window = new WebWindow(node.Network.CoinTicker + " Node");
                window.NavigateToUrl(node.NodeService<ApiSettings>().ApiUri.ToString());
                window.WaitForExit();

                node.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex.ToString());
            }
        }

        public static void HideConsole()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var handle = GetConsoleWindow();

                // Hide
                ShowWindow(handle, SW_HIDE);

                // Show
                // ShowWindow(handle, SW_SHOW);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
            }
            else
            {
                // Do nothing
            }
        }
    }
}