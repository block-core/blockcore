using System.Linq;
using System.Threading.Tasks;
using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Builder.Feature;
using Blockcore.Configuration.Logging;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Miner;
using Blockcore.Features.PoA.Behaviors;
using Blockcore.Features.PoA.Voting;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol.Behaviors;
using Blockcore.P2P.Protocol.Payloads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.PoA
{
    public class PoAFeature : FullNodeFeature
    {
        /// <summary>Manager of node's network connections.</summary>
        private readonly IConnectionManager connectionManager;

        /// <summary>Thread safe chain of block headers from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        private readonly IFederationManager federationManager;

        /// <summary>Provider of IBD state.</summary>
        private readonly IInitialBlockDownloadState initialBlockDownloadState;

        private readonly IConsensusManager consensusManager;

        /// <summary>A handler that can manage the lifetime of network peers.</summary>
        private readonly IPeerBanning peerBanning;

        /// <summary>Factory for creating loggers.</summary>
        private readonly ILoggerFactory loggerFactory;

        private readonly IPoAMiner miner;

        private readonly VotingManager votingManager;

        private readonly Network network;

        private readonly IWhitelistedHashesRepository whitelistedHashesRepository;

        private readonly IdleFederationMembersKicker idleFederationMembersKicker;

        private readonly IChainState chainState;

        private readonly IBlockStoreQueue blockStoreQueue;

        public PoAFeature(IFederationManager federationManager, PayloadProvider payloadProvider, IConnectionManager connectionManager, ChainIndexer chainIndexer,
            IInitialBlockDownloadState initialBlockDownloadState, IConsensusManager consensusManager, IPeerBanning peerBanning, ILoggerFactory loggerFactory,
            IPoAMiner miner, VotingManager votingManager, Network network, IWhitelistedHashesRepository whitelistedHashesRepository,
            IdleFederationMembersKicker idleFederationMembersKicker, IChainState chainState, IBlockStoreQueue blockStoreQueue)
        {
            this.federationManager = federationManager;
            this.connectionManager = connectionManager;
            this.chainIndexer = chainIndexer;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.consensusManager = consensusManager;
            this.peerBanning = peerBanning;
            this.loggerFactory = loggerFactory;
            this.miner = miner;
            this.votingManager = votingManager;
            this.whitelistedHashesRepository = whitelistedHashesRepository;
            this.network = network;
            this.idleFederationMembersKicker = idleFederationMembersKicker;
            this.chainState = chainState;
            this.blockStoreQueue = blockStoreQueue;

            payloadProvider.DiscoverPayloads(this.GetType().Assembly);
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            NetworkPeerConnectionParameters connectionParameters = this.connectionManager.Parameters;

            this.ReplaceConsensusManagerBehavior(connectionParameters);

            this.ReplaceBlockStoreBehavior(connectionParameters);

            this.federationManager.Initialize();
            this.whitelistedHashesRepository.Initialize();

            var options = (PoAConsensusOptions)this.network.Consensus.Options;

            if (options.VotingEnabled)
            {
                this.votingManager.Initialize();

                if (options.AutoKickIdleMembers)
                    this.idleFederationMembersKicker.Initialize();
            }

            this.miner.InitializeMining();

            return Task.CompletedTask;
        }

        /// <summary>Replaces default <see cref="ConsensusManagerBehavior"/> with <see cref="PoAConsensusManagerBehavior"/>.</summary>
        private void ReplaceConsensusManagerBehavior(NetworkPeerConnectionParameters connectionParameters)
        {
            INetworkPeerBehavior defaultConsensusManagerBehavior = connectionParameters.TemplateBehaviors.FirstOrDefault(behavior => behavior is ConsensusManagerBehavior);

            if (defaultConsensusManagerBehavior == null)
            {
                throw new MissingServiceException(typeof(ConsensusManagerBehavior), "Missing expected ConsensusManagerBehavior.");
            }

            connectionParameters.TemplateBehaviors.Remove(defaultConsensusManagerBehavior);
            connectionParameters.TemplateBehaviors.Add(new PoAConsensusManagerBehavior(this.chainIndexer, this.initialBlockDownloadState, this.consensusManager, this.peerBanning, this.loggerFactory));
        }

        /// <summary>Replaces default <see cref="PoABlockStoreBehavior"/> with <see cref="PoABlockStoreBehavior"/>.</summary>
        private void ReplaceBlockStoreBehavior(NetworkPeerConnectionParameters connectionParameters)
        {
            INetworkPeerBehavior defaultBlockStoreBehavior = connectionParameters.TemplateBehaviors.FirstOrDefault(behavior => behavior is BlockStoreBehavior);

            if (defaultBlockStoreBehavior == null)
            {
                throw new MissingServiceException(typeof(BlockStoreBehavior), "Missing expected BlockStoreBehavior.");
            }

            connectionParameters.TemplateBehaviors.Remove(defaultBlockStoreBehavior);
            connectionParameters.TemplateBehaviors.Add(new PoABlockStoreBehavior(this.chainIndexer, this.chainState, this.loggerFactory, this.consensusManager, this.blockStoreQueue));
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.miner.Dispose();

            this.votingManager.Dispose();

            this.idleFederationMembersKicker.Dispose();
        }
    }

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderConsensusExtension
    {
        /// <summary>This is mandatory for all PoA networks.</summary>
        public static IFullNodeBuilder UsePoAConsensus(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PoAFeature>()
                    .DependOn<ConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<IFederationManager, FederationManager>();
                        services.AddSingleton<PoABlockHeaderValidator>();
                        services.AddSingleton<IPoAMiner, PoAMiner>();
                        services.AddSingleton<MinerSettings>();
                        services.AddSingleton<PoAMinerSettings>();
                        services.AddSingleton<ISlotsManager, SlotsManager>();
                        services.AddSingleton<BlockDefinition, PoABlockDefinition>();
                    });
            });

            LoggingConfiguration.RegisterFeatureNamespace<ConsensusFeature>("consensus");
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<ConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<DBreezeCoindb>();
                        services.AddSingleton<ICoinView, CachedCoinView>();
                        services.AddSingleton<IConsensusRuleEngine, PoAConsensusRuleEngine>();
                        services.AddSingleton<IChainState, ChainState>();
                        services.AddSingleton<ConsensusQuery>()
                            .AddSingleton<INetworkDifficulty, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>())
                            .AddSingleton<IGetUnspentTransaction, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>());

                        // Voting.
                        services.AddSingleton<VotingManager>();
                        services.AddSingleton<IPollResultExecutor, PollResultExecutor>();
                        services.AddSingleton<IWhitelistedHashesRepository, WhitelistedHashesRepository>();
                        services.AddSingleton<IdleFederationMembersKicker>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
