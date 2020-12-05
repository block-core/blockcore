using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration.Logging;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Features.BlockStore.AddressIndexing;
using Blockcore.Features.BlockStore.Pruning;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Utilities;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Blockcore.Features.BlockStore.Tests")]

namespace Blockcore.Features.BlockStore
{
    public class BlockStoreFeature : FullNodeFeature
    {
        private readonly Network network;
        private readonly ChainIndexer chainIndexer;

        private readonly BlockStoreSignaled blockStoreSignaled;

        private readonly IConnectionManager connectionManager;

        private readonly StoreSettings storeSettings;

        private readonly IChainState chainState;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Factory for creating loggers.</summary>
        private readonly ILoggerFactory loggerFactory;

        private readonly IBlockStoreQueue blockStoreQueue;

        private readonly IConsensusManager consensusManager;

        private readonly ICheckpoints checkpoints;

        private readonly IPrunedBlockRepository prunedBlockRepository;

        private readonly IAddressIndexer addressIndexer;
        private readonly IPruneBlockStoreService pruneBlockStoreService;

        public BlockStoreFeature(
            Network network,
            ChainIndexer chainIndexer,
            IConnectionManager connectionManager,
            BlockStoreSignaled blockStoreSignaled,
            ILoggerFactory loggerFactory,
            StoreSettings storeSettings,
            IChainState chainState,
            IBlockStoreQueue blockStoreQueue,
            INodeStats nodeStats,
            IConsensusManager consensusManager,
            ICheckpoints checkpoints,
            IPrunedBlockRepository prunedBlockRepository,
            IAddressIndexer addressIndexer,
            IPruneBlockStoreService pruneBlockStoreService)
        {
            this.network = network;
            this.chainIndexer = chainIndexer;
            this.blockStoreQueue = blockStoreQueue;
            this.blockStoreSignaled = blockStoreSignaled;
            this.connectionManager = connectionManager;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.loggerFactory = loggerFactory;
            this.storeSettings = storeSettings;
            this.chainState = chainState;
            this.consensusManager = consensusManager;
            this.checkpoints = checkpoints;
            this.prunedBlockRepository = prunedBlockRepository;
            this.addressIndexer = addressIndexer;
            this.pruneBlockStoreService = pruneBlockStoreService;
            nodeStats.RegisterStats(this.AddInlineStats, StatsType.Inline, this.GetType().Name, 900);
        }

        private void AddInlineStats(StringBuilder log)
        {
            ChainedHeader highestBlock = this.chainState.BlockStoreTip;

            if (highestBlock != null)
            {
                var builder = new StringBuilder();
                builder.Append("BlockStore.Height: ".PadRight(LoggingConfiguration.ColumnLength + 1) + highestBlock.Height.ToString().PadRight(8));
                builder.Append(" BlockStore.Hash: ".PadRight(LoggingConfiguration.ColumnLength - 1) + highestBlock.HashBlock);
                log.AppendLine(builder.ToString());

                if (this.storeSettings.PruningEnabled)
                {
                    builder = new StringBuilder();
                    var prunedTip = this.prunedBlockRepository.PrunedTip;
                    builder.Append("PrunedStore.Height: ".PadRight(LoggingConfiguration.ColumnLength + 1) + prunedTip.Height.ToString().PadRight(8));
                    builder.Append(" PrunedStore.Hash: ".PadRight(LoggingConfiguration.ColumnLength - 1) + prunedTip.Hash);
                    log.AppendLine(builder.ToString());
                }
            }
        }

        /// <summary>
        /// Prints command-line help. Invoked via reflection.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            StoreSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration. Invoked via reflection.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            StoreSettings.BuildDefaultConfigurationFile(builder, network);
        }

        public override Task InitializeAsync()
        {
            this.prunedBlockRepository.Initialize();

            if (!this.storeSettings.PruningEnabled && this.prunedBlockRepository.PrunedTip != null)
                throw new BlockStoreException("The node cannot start as it has been previously pruned, please clear the data folders and resync.");

            if (this.storeSettings.PruningEnabled)
            {
                if (this.storeSettings.AmountOfBlocksToKeep < this.network.Consensus.MaxReorgLength)
                    throw new BlockStoreException($"The amount of blocks to prune [{this.storeSettings.AmountOfBlocksToKeep}] (blocks to keep) cannot be less than the node's max reorg length of {this.network.Consensus.MaxReorgLength}.");

                this.prunedBlockRepository.PrepareDatabase();

                this.logger.LogInformation("Starting Prunning...");
                this.pruneBlockStoreService.Initialize();
            }

            // Use ProvenHeadersBlockStoreBehavior for PoS Networks
            if (this.network.Consensus.IsProofOfStake)
            {
                this.connectionManager.Parameters.TemplateBehaviors.Add(new ProvenHeadersBlockStoreBehavior(this.network, this.chainIndexer, this.chainState, this.loggerFactory, this.consensusManager, this.checkpoints, this.blockStoreQueue));
            }
            else
            {
                this.connectionManager.Parameters.TemplateBehaviors.Add(new BlockStoreBehavior(this.chainIndexer, this.chainState, this.loggerFactory, this.consensusManager, this.blockStoreQueue));
            }

            // Signal to peers that this node can serve blocks.
            // TODO: Add NetworkLimited which is what BTC uses for pruned nodes.
            this.connectionManager.Parameters.Services = (this.storeSettings.PruningEnabled ? NetworkPeerServices.Nothing : NetworkPeerServices.Network);

            this.blockStoreSignaled.Initialize();

            this.addressIndexer.Initialize();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (this.storeSettings.PruningEnabled)
            {
                this.logger.LogInformation("Stopping Prunning...");
                this.pruneBlockStoreService.Dispose();
            }

            this.logger.LogInformation("Stopping BlockStoreSignaled.");
            this.blockStoreSignaled.Dispose();

            this.logger.LogInformation("Stopping AddressIndexer.");
            this.addressIndexer.Dispose();
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderBlockStoreExtension
    {
        public static IFullNodeBuilder UseBlockStore(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<BlockStoreFeature>("db");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<BlockStoreFeature>()
                .FeatureServices(services =>
                    {
                        services.AddSingleton<IBlockStoreQueue, BlockStoreQueue>().AddSingleton<IBlockStore>(provider => provider.GetService<IBlockStoreQueue>());

                        fullNodeBuilder.PersistenceProviderManager.RequirePersistence<BlockStoreFeature>(services);

                        if (fullNodeBuilder.Network.Consensus.IsProofOfStake)
                            services.AddSingleton<BlockStoreSignaled, ProvenHeadersBlockStoreSignaled>();
                        else
                            services.AddSingleton<BlockStoreSignaled>();

                        services.AddSingleton<StoreSettings>();
                        services.AddSingleton<IBlockStoreQueueFlushCondition, BlockStoreQueueFlushCondition>();
                        services.AddSingleton<IAddressIndexer, AddressIndexer>();
                        services.AddSingleton<IUtxoIndexer, UtxoIndexer>();

                        services.AddSingleton<IPruneBlockStoreService, PruneBlockStoreService>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}