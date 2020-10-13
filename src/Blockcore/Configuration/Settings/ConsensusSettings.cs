using System.Text;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Configuration.Settings
{
    /// <summary>
    /// Configurable settings for the consensus feature.
    /// </summary>
    public class ConsensusSettings
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Whether use of checkpoints is enabled or not.</summary>
        public bool UseCheckpoints { get; set; }

        /// <summary>
        /// If this block is in the chain assume that it and its ancestors are valid and skip their script verification.
        /// Null to not assume valid blocks and therefore validate all blocks.
        /// </summary>
        public uint256 BlockAssumedValid { get; set; }

        /// <summary>Maximum tip age in seconds to consider node in initial block download.</summary>
        public int MaxTipAge { get; private set; }

        /// <summary>
        /// Maximum memory to use for unconsume blocks in MB.
        /// Used in consensus manager to set <seealso cref="ConsensusManager.MaxUnconsumedBlocksDataBytes"/>
        /// </summary>
        public int MaxBlockMemoryInMB { get; private set; }

        /// <summary>
        /// Maximum memory to use for the coin db cache .
        /// </summary>
        public int MaxCoindbCacheInMB { get; private set; }

        /// <summary>
        /// How often to flush the cache to disk when in IBD, note if dbcache is bigger then <see cref="MaxCoindbCacheInMB"/> flush will happen anyway happen.
        /// </summary>
        public int CoindbIbdFlushMin { get; private set; }

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="nodeSettings">The node configuration.</param>
        public ConsensusSettings(NodeSettings nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            this.logger = nodeSettings.LoggerFactory.CreateLogger(typeof(ConsensusSettings).FullName);

            TextFileConfiguration config = nodeSettings.ConfigReader;

            this.UseCheckpoints = config.GetOrDefault<bool>("checkpoints", true, this.logger);
            this.BlockAssumedValid = config.GetOrDefault<uint256>("assumevalid", nodeSettings.Network.Consensus.DefaultAssumeValid, this.logger);
            this.MaxTipAge = config.GetOrDefault("maxtipage", nodeSettings.Network.MaxTipAge, this.logger);
            this.MaxBlockMemoryInMB = config.GetOrDefault("maxblkmem", 200, this.logger);
            this.MaxCoindbCacheInMB = config.GetOrDefault("dbcache", 200, this.logger);
            this.CoindbIbdFlushMin = config.GetOrDefault("dbflush", 10, this.logger);
        }

        /// <summary>Prints the help information on how to configure the Consensus settings to the logger.</summary>
        /// <param name="network">The network to use.</param>
        public static void PrintHelp(Network network)
        {
            Guard.NotNull(network, nameof(network));

            var builder = new StringBuilder();

            builder.AppendLine($"-checkpoints=<0 or 1>     Use checkpoints. Default 1.");
            builder.AppendLine($"-assumevalid=<hex>        If this block is in the chain assume that it and its ancestors are valid and potentially skip their script verification (0 to verify all). Defaults to { network.Consensus.DefaultAssumeValid }.");
            builder.AppendLine($"-maxtipage=<number>       Max tip age. Default {network.MaxTipAge}.");
            builder.AppendLine($"-maxblkmem=<number>       Max memory to use for unconsumed blocks in MB. Default 200 (this does not include the size of objects in memory).");
            builder.AppendLine($"-dbcache=<number>         Max cache memory for the coindb in MB. Default 200 (this does not include the size of objects in memory).");
            builder.AppendLine($"-dbflush=<number>         How often to flush the cache to disk when in IBD in minutes. Default 10 min (min=1min, max=60min).");

            NodeSettings.Default(network).Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            builder.AppendLine("####Consensus Settings####");
            builder.AppendLine($"#Use checkpoints. Default 1.");
            builder.AppendLine($"#checkpoints=1");
            builder.AppendLine($"#If this block is in the chain assume that it and its ancestors are valid and potentially skip their script verification (0 to verify all). Defaults to { network.Consensus.DefaultAssumeValid }.");
            builder.AppendLine($"#assumevalid={network.Consensus.DefaultAssumeValid}");
            builder.AppendLine($"#Max tip age. Default {network.MaxTipAge}.");
            builder.AppendLine($"#maxtipage={network.MaxTipAge}");
            builder.AppendLine($"#Max memory to use for unconsumed blocks in MB. Default 200.");
            builder.AppendLine($"#maxblkmem=200");
            builder.AppendLine($"#Max cache memory for the coindb in MB. Default 200.");
            builder.AppendLine($"#dbcache=200");
            builder.AppendLine($"#How often to flush the cache to disk when in IBD in minutes (min=1min, max=60min). The bigger the number the faster the sync and smaller the db, but shutdown will be longer.");
            builder.AppendLine($"#dbflush=10");
        }
    }
}
