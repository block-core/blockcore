using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration.Logging;
using Blockcore.Connection;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool.Broadcasting;
using Blockcore.Features.MemoryPool.Fee;
using Blockcore.Features.MemoryPool.FeeFilter;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NBitcoin;

[assembly: InternalsVisibleTo("Blockcore.Features.MemoryPool.Tests")]

namespace Blockcore.Features.MemoryPool
{
    /// <summary>
    /// Transaction memory pool feature for the Full Node.
    /// </summary>
    /// <seealso cref="https://github.com/bitcoin/bitcoin/blob/6dbcc74a0e0a7d45d20b03bb4eb41a027397a21d/src/txmempool.cpp"/>
    public class MempoolFeature : FullNodeFeature
    {
        private readonly IConnectionManager connectionManager;
        private readonly MempoolSignaled mempoolSignaled;
        private readonly BlocksDisconnectedSignaled blocksDisconnectedSignaled;
        private readonly MempoolBehavior mempoolBehavior;
        private readonly MempoolManager mempoolManager;
        private readonly IBroadcasterManager broadcasterManager;
        private readonly PayloadProvider payloadProvider;
        private readonly FeeFilterBehavior feeFilterBehavior;
        private readonly ILogger logger;

        public MempoolFeature(
            IConnectionManager connectionManager,
            MempoolSignaled mempoolSignaled,
            BlocksDisconnectedSignaled blocksDisconnectedSignaled,
            MempoolBehavior mempoolBehavior,
            MempoolManager mempoolManager,
            ILoggerFactory loggerFactory,
            INodeStats nodeStats,
            IBroadcasterManager broadcasterManager,
            PayloadProvider payloadProvider,
            FeeFilterBehavior feeFilterBehavior)
        {
            this.connectionManager = connectionManager;
            this.mempoolSignaled = mempoolSignaled;
            this.blocksDisconnectedSignaled = blocksDisconnectedSignaled;
            this.mempoolBehavior = mempoolBehavior;
            this.mempoolManager = mempoolManager;
            this.broadcasterManager = broadcasterManager;
            this.payloadProvider = payloadProvider;
            this.feeFilterBehavior = feeFilterBehavior;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);

            nodeStats.RegisterStats(this.AddComponentStats, StatsType.Component, this.GetType().Name);
        }

        private void AddComponentStats(StringBuilder log)
        {
            if (this.mempoolManager != null)
            {
                log.AppendLine();
                log.AppendLine("=======Mempool=======");
                log.AppendLine(this.mempoolManager.PerformanceCounter.ToString());
            }
        }

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            await this.mempoolManager.LoadPoolAsync().ConfigureAwait(false);

            this.connectionManager.Parameters.TemplateBehaviors.Add(this.mempoolBehavior);
            this.connectionManager.Parameters.TemplateBehaviors.Add(this.feeFilterBehavior);

            this.mempoolSignaled.Start();

            this.blocksDisconnectedSignaled.Initialize();

            // The mempool responds to trx getdata so disbled it form the broadcaster
            this.broadcasterManager.CanRespondToTrxGetData = false;

            // Register the fee filter.
            this.payloadProvider.AddPayload(typeof(FeeFilterPayload));
        }

        /// <summary>
        /// Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(Network network)
        {
            MempoolSettings.PrintHelp(network);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, Network network)
        {
            MempoolSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.logger.LogInformation("Saving Memory Pool.");

            MemPoolSaveResult result = this.mempoolManager.SavePool();
            if (result.Succeeded)
            {
                this.logger.LogInformation($"Memory Pool Saved {result.TrxSaved} transactions");
            }
            else
            {
                this.logger.LogWarning("Memory Pool Not Saved!");
            }

            this.blocksDisconnectedSignaled.Dispose();

            this.mempoolSignaled.Stop();
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderMempoolExtension
    {
        /// <summary>
        /// Include the memory pool feature and related services in the full node.
        /// </summary>
        /// <param name="fullNodeBuilder">Full node builder.</param>
        /// <returns>Full node builder.</returns>
        public static IFullNodeBuilder UseMempool(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<MempoolFeature>("mempool");
            LoggingConfiguration.RegisterFeatureNamespace<BlockPolicyEstimator>("estimatefee");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<MempoolFeature>()
                .DependOn<ConsensusFeature>()
                .FeatureServices(services =>
                    {
                        services.AddSingleton<MempoolSchedulerLock>();
                        services.AddSingleton<ITxMempool, TxMempool>();
                        services.AddSingleton<BlockPolicyEstimator>();
                        services.AddSingleton<IMempoolValidator, MempoolValidator>();
                        services.AddSingleton<MempoolOrphans>();
                        services.AddSingleton<MempoolManager>()
                            .AddSingleton<IPooledTransaction, MempoolManager>(provider => provider.GetService<MempoolManager>())
                            .AddSingleton<IPooledGetUnspentTransaction, MempoolManager>(provider => provider.GetService<MempoolManager>());
                        services.AddSingleton<MempoolBehavior>();
                        services.AddSingleton<FeeFilterBehavior>();
                        services.AddSingleton<MempoolSignaled>();
                        services.AddSingleton<BlocksDisconnectedSignaled>();
                        services.AddSingleton<IMempoolPersistence, MempoolPersistence>();
                        services.AddSingleton<MempoolSettings>();
                        services.AddSingleton<IBroadcastCheck, MempoolBroadcastCheck>();

                        foreach (var ruleType in fullNodeBuilder.Network.Consensus.MempoolRules)
                            services.AddSingleton(typeof(IMempoolRule), ruleType);
                    });
            });

            return fullNodeBuilder;
        }
    }
}