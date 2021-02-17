using System.Text;
using Blockcore.Configuration;
using Blockcore.Features.Miner;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.X1.Components
{
    /// <summary>
    /// Configuration related to the miner interface.
    /// </summary>
    public class X1MinerSettings : MinerSettings
    {
        private const ulong MinimumSplitCoinValueDefaultValue = 100 * Money.COIN;

        private const ulong MinimumStakingCoinValueDefaultValue = 10 * Money.CENT;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// Set the threads for CPU mining.
        /// </summary>
        public int MineThreadCount { get; }

        /// <summary>
        /// Use a GPU to mine if available, Default true.
        /// </summary>
        public bool UseOpenCL { get; }

        /// <summary>
        /// The name of the OpenCLDevice to use. Default is first one found.
        /// </summary>
        public string OpenCLDevice { get; set; }

        /// <summary>
        /// Amount to split the work to send to the OpenCL device.
        /// Experiment with this value to find the optimum between a short execution time and big hash rate.
        /// </summary>
        public int OpenCLWorksizeSplit { get; }

       

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="nodeSettings">The node configuration.</param>
        public X1MinerSettings(NodeSettings nodeSettings) : base(nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            this.logger = nodeSettings.LoggerFactory.CreateLogger(typeof(X1MinerSettings).FullName);

            TextFileConfiguration config = nodeSettings.ConfigReader;

            if (this.Mine)
            {
                this.MineThreadCount = config.GetOrDefault("minethreads",1, this.logger);
                this.UseOpenCL = config.GetOrDefault("useopencl", false, this.logger);
                this.OpenCLDevice = config.GetOrDefault("opencldevice", string.Empty, this.logger);
                this.OpenCLWorksizeSplit = config.GetOrDefault("openclworksizesplit", 10, this.logger);
            }
        }

        /// <summary>
        /// Displays mining help information on the console.
        /// </summary>
        /// <param name="network">Not used.</param>
        public new static void PrintHelp(Network network)
        {
            NodeSettings defaults = NodeSettings.Default(network);
            var builder = new StringBuilder();

            builder.AppendLine("-mine=<0 or 1>                      Enable POW mining.");
            builder.AppendLine("-mineaddress=<string>               The address to use for mining (empty string to select an address from the wallet).");
            builder.AppendLine("-minethreads=1                      Total threads to mine on (default 1).");
            builder.AppendLine("-useopencl=<0 or 1>                 Use OpenCL for POW mining (default 0)");
            builder.AppendLine("-opencldevice=<string>              Name of the OpenCL device to use (default first available).");
            builder.AppendLine("-openclworksizesplit=<number>       Default 10. Amount to split the work to send to the OpenCL device. Experiment with this value to find the optimum between a short execution time and big hash rate.");

            builder.AppendLine("-stake=<0 or 1>                     Enable POS.");
            builder.AppendLine("-mineaddress=<string>               The address to use for mining (empty string to select an address from the wallet).");
            builder.AppendLine("-walletname=<string>                The wallet name to use when staking.");
            builder.AppendLine("-walletpassword=<string>            Password to unlock the wallet.");
            builder.AppendLine("-blockmaxsize=<number>              Maximum block size (in bytes) for the miner to generate.");
            builder.AppendLine("-blockmaxweight=<number>            Maximum block weight (in weight units) for the miner to generate.");
            builder.AppendLine("-blockmintxfee=<number>             Minimum fee rate for transactions to be included in blocks created by miner.");
            builder.AppendLine("-enablecoinstakesplitting=<0 or 1>  Enable splitting coins when staking. This is true by default.");
            builder.AppendLine($"-minimumstakingcoinvalue=<number>   Minimum size of the coins considered for staking, in satoshis. Default value is {MinimumStakingCoinValueDefaultValue:N0} satoshis (= {MinimumStakingCoinValueDefaultValue / (decimal)Money.COIN:N1} Coin).");
            builder.AppendLine($"-minimumsplitcoinvalue=<number>     Targeted minimum value of staking coins after splitting, in satoshis. Default value is {MinimumSplitCoinValueDefaultValue:N0} satoshis (= {MinimumSplitCoinValueDefaultValue / Money.COIN} Coin).");

            builder.AppendLine($"-enforceStakingFlag=<0 or 1>        If true staking will require whitelisting addresses in order to stake. Defult is false");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public new static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            builder.AppendLine("####Miner Settings####");
            builder.AppendLine("#Enable POW mining.");
            builder.AppendLine("#mine=0");
            builder.AppendLine("#Enable POS.");
            builder.AppendLine("#stake=0");
            builder.AppendLine("#The address to use for mining (empty string to select an address from the wallet).");
            builder.AppendLine("#mineaddress=<string>");
            builder.AppendLine("#Total threads to mine on (default 1)..");
            builder.AppendLine("#minethreads=1");
            builder.AppendLine("#Use OpenCL for POW mining.");
            builder.AppendLine("#useopencl=0");
            builder.AppendLine("#Name of the OpenCL device to use (defaults to first available).");
            builder.AppendLine("#opencldevice=<string>");
            builder.AppendLine("#Amount to split the work to send to the OpenCL device. Experiment with this value to find the optimum between a short execution time and big hash rate.");
            builder.AppendLine("#openclworksizesplit=10");
            builder.AppendLine("#The wallet name to use when staking.");
            builder.AppendLine("#walletname=<string>");
            builder.AppendLine("#Password to unlock the wallet.");
            builder.AppendLine("#walletpassword=<string>");
            builder.AppendLine("#Maximum block size (in bytes) for the miner to generate.");
            builder.AppendLine($"#blockmaxsize={network.Consensus.Options.MaxBlockSerializedSize}");
            builder.AppendLine("#Maximum block weight (in weight units) for the miner to generate.");
            builder.AppendLine($"#blockmaxweight={network.Consensus.Options.MaxBlockWeight}");
            builder.AppendLine("#Minimum fee rate for transactions to be included in blocks created by miner.");
            builder.AppendLine($"#blockmintxfee={network.Consensus.Options.MinBlockFeeRate}");
            builder.AppendLine("#Enable splitting coins when staking.");
            builder.AppendLine("#enablecoinstakesplitting=1");
            builder.AppendLine("#Minimum size of the coins considered for staking, in satoshis.");
            builder.AppendLine($"#minimumstakingcoinvalue={MinimumStakingCoinValueDefaultValue}");
            builder.AppendLine("#Targeted minimum value of staking coins after splitting, in satoshis.");
            builder.AppendLine($"#minimumsplitcoinvalue={MinimumSplitCoinValueDefaultValue}");
            builder.AppendLine("#If staking will require whitelisting addresses in order to stake. Defult is false.");
            builder.AppendLine($"#enforceStakingFlag=0");

        }
    }
}
