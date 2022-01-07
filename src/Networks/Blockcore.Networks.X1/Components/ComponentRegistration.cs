using Blockcore.Base;
using Blockcore.Broadcasters;
using Blockcore.Builder;
using Blockcore.Configuration.Logging;
using Blockcore.Consensus;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Consensus.Interfaces;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Features.Consensus.Rules;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.Miner.Broadcasters;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet.UI;
using Blockcore.Interfaces;
using Blockcore.Interfaces.UI;
using Blockcore.Mining;
using Microsoft.Extensions.DependencyInjection;

namespace Blockcore.Networks.X1.Components
{
    public static class ComponentRegistration
    {
        public static IFullNodeBuilder UseX1Consensus(this IFullNodeBuilder fullNodeBuilder)
        {
            return AddX1PowPosMining(UseX1PosConsensus(fullNodeBuilder));
        }

        static IFullNodeBuilder UseX1PosConsensus(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<PosConsensusFeature>("posconsensus");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PosConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        fullNodeBuilder.PersistenceProviderManager.RequirePersistence<PosConsensusFeature>(services);

                        services.AddSingleton<IStakdb>(provider => (IStakdb)provider.GetService<ICoindb>());
                        services.AddSingleton<ICoinView, CachedCoinView>();
                        services.AddSingleton<StakeChainStore>().AddSingleton<IStakeChain, StakeChainStore>(provider => provider.GetService<StakeChainStore>());
                        services.AddSingleton<IRewindDataIndexCache, RewindDataIndexCache>();
                        services.AddSingleton<IConsensusRuleEngine, PosConsensusRuleEngine>();
                        services.AddSingleton<IChainState, ChainState>();
                        services.AddSingleton<ConsensusQuery>()
                            .AddSingleton<INetworkDifficulty, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>())
                            .AddSingleton<IGetUnspentTransaction, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>());
                        services.AddSingleton<IProvenBlockHeaderStore, ProvenBlockHeaderStore>();

                        services.AddSingleton<IStakeValidator, X1StakeValidator>();
                    });
            });

            return fullNodeBuilder;
        }

        /// <summary>
        /// Adds POW and POS miner components to the node, so that it can mine or stake.
        /// </summary>
        /// <param name="fullNodeBuilder">The object used to build the current node.</param>
        /// <returns>The full node builder, enriched with the new component.</returns>
        static IFullNodeBuilder AddX1PowPosMining(this IFullNodeBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<X1MiningFeature>("x1mining");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<X1MiningFeature>()
                    .DependOn<MempoolFeature>()
                    .DependOn<RPCFeature>()
                    // TODO: Need a better way to check dependencies. This is really just dependent on IWalletManager...
                    // Alternatively "DependsOn" should take a list of features that will satisfy the dependency.
                    //.DependOn<WalletFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<MinerSettings, X1MinerSettings>();
                        services.AddSingleton<IPowMining, X1PowMining>();
                        services.AddSingleton<IPosMinting, X1PosMinting>();

                        services.AddSingleton<IBlockProvider, BlockProvider>();
                        services.AddSingleton<BlockDefinition, PowBlockDefinition>();
                        services.AddSingleton<BlockDefinition, PosBlockDefinition>();
                        services.AddSingleton<BlockDefinition, PosPowBlockDefinition>();
                        services.AddSingleton<INavigationItem, StakeNavigationItem>();
                        services.AddSingleton<IClientEventBroadcaster, StakingBroadcaster>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}
