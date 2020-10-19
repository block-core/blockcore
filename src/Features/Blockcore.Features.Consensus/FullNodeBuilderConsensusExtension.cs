using System;
using Blockcore.Base;
using Blockcore.Builder;
using Blockcore.Configuration;
using Blockcore.Configuration.Logging;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Consensus.Interfaces;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Features.Consensus.Rules;
using Blockcore.Interfaces;
using Blockcore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;

namespace Blockcore.Features.Consensus
{
    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class FullNodeBuilderConsensusExtension
    {
        public static IFullNodeBuilder UsePowConsensus(this IFullNodeBuilder fullNodeBuilder, DbType coindbType = DbType.Rocksdb)
        {
            LoggingConfiguration.RegisterFeatureNamespace<PowConsensusFeature>("powconsensus");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PowConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        AddDbImplementation(services, fullNodeBuilder.NodeSettings);
                        services.AddSingleton<ConsensusOptions, ConsensusOptions>();
                        services.AddSingleton<ICoinView, CachedCoinView>();
                        services.AddSingleton<IConsensusRuleEngine, PowConsensusRuleEngine>();
                        services.AddSingleton<IChainState, ChainState>();
                        services.AddSingleton<ConsensusQuery>()
                            .AddSingleton<INetworkDifficulty, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>())
                            .AddSingleton<IGetUnspentTransaction, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>());
                    });
            });

            return fullNodeBuilder;
        }

        public static IFullNodeBuilder UsePosConsensus(this IFullNodeBuilder fullNodeBuilder, DbType coindbType = DbType.Rocksdb)
        {
            LoggingConfiguration.RegisterFeatureNamespace<PosConsensusFeature>("posconsensus");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PosConsensusFeature>()
                    .FeatureServices(services =>
                    {
                        AddDbImplementation(services, fullNodeBuilder.NodeSettings);
                        services.AddSingleton<IStakdb>(provider => (IStakdb)provider.GetService<ICoindb>());
                        services.AddSingleton<ICoinView, CachedCoinView>();
                        services.AddSingleton<StakeChainStore>().AddSingleton<IStakeChain, StakeChainStore>(provider => provider.GetService<StakeChainStore>());
                        services.AddSingleton<IStakeValidator, StakeValidator>();
                        services.AddSingleton<IRewindDataIndexCache, RewindDataIndexCache>();
                        services.AddSingleton<IConsensusRuleEngine, PosConsensusRuleEngine>();
                        services.AddSingleton<IChainState, ChainState>();
                        services.AddSingleton<ConsensusQuery>()
                            .AddSingleton<INetworkDifficulty, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>())
                            .AddSingleton<IGetUnspentTransaction, ConsensusQuery>(provider => provider.GetService<ConsensusQuery>());
                        services.AddSingleton<IProvenBlockHeaderStore, ProvenBlockHeaderStore>();
                        services.AddSingleton<IProvenBlockHeaderRepository, ProvenBlockHeaderRepository>();
                    });
            });

            return fullNodeBuilder;
        }

        private static void AddDbImplementation(IServiceCollection services, NodeSettings settings)
        {
            if (settings.DbType == DbType.Leveldb)
            {
                services.AddSingleton<ICoindb, LeveldbCoindb>();
                return;
            }

            if (settings.DbType == DbType.Rocksdb)
            {
                services.AddSingleton<ICoindb, RocksdbCoindb>();
                return;
            }
        }
    }
}