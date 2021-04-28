using System.Collections.Generic;
using System.Linq;
using Blockcore.AsyncWork;
using Blockcore.Base;
using Blockcore.Base.Deployments;
using Blockcore.Configuration;
using Blockcore.Configuration.Logging;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.Rules;
using Blockcore.Features.Base.Persistence.LevelDb;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.PoA.Voting;
using Blockcore.Networks;
using Blockcore.Signals;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;

namespace Blockcore.Features.PoA.Tests
{
    public class PoATestsBase
    {
        protected readonly ChainedHeader currentHeader;
        protected readonly TestPoANetwork network;
        protected readonly PoAConsensusOptions consensusOptions;

        protected PoAConsensusRuleEngine rulesEngine;
        protected readonly LoggerFactory loggerFactory;
        protected readonly PoABlockHeaderValidator poaHeaderValidator;
        protected readonly ISlotsManager slotsManager;
        protected readonly ConsensusSettings consensusSettings;
        protected readonly ChainIndexer ChainIndexer;
        protected readonly IFederationManager federationManager;
        protected readonly VotingManager votingManager;
        protected readonly Mock<IPollResultExecutor> resultExecutorMock;
        protected readonly Mock<ChainIndexer> chainIndexerMock;
        protected readonly ISignals signals;
        protected readonly DataStoreSerializer DataStoreSerializer;
        protected readonly ChainState chainState;
        protected readonly IAsyncProvider asyncProvider;

        public PoATestsBase(TestPoANetwork network = null)
        {
            this.loggerFactory = new LoggerFactory();
            this.signals = new Signals.Signals(this.loggerFactory, null);
            this.network = network == null ? new TestPoANetwork() : network;
            this.consensusOptions = this.network.ConsensusOptions;
            this.DataStoreSerializer = new DataStoreSerializer(this.network.Consensus.ConsensusFactory);

            this.ChainIndexer = new ChainIndexer(this.network);
            IDateTimeProvider timeProvider = new DateTimeProvider();
            this.consensusSettings = new ConsensusSettings(NodeSettings.Default(this.network));

            this.federationManager = CreateFederationManager(this, this.network, this.loggerFactory, this.signals);

            this.chainIndexerMock = new Mock<ChainIndexer>();
            var header = new BlockHeader();
            this.chainIndexerMock.Setup(x => x.Tip).Returns(new ChainedHeader(header, header.GetHash(), 0));
            this.slotsManager = new SlotsManager(this.network, this.federationManager, this.chainIndexerMock.Object, this.loggerFactory);

            this.poaHeaderValidator = new PoABlockHeaderValidator(this.loggerFactory);
            this.asyncProvider = new AsyncProvider(this.loggerFactory, this.signals, new Mock<INodeLifetime>().Object);

            var dataFolder = new DataFolder(TestBase.CreateTestDir(this));
            var finalizedBlockRepo = new FinalizedBlockInfoRepository(new LevelDbKeyValueRepository(dataFolder, this.DataStoreSerializer), this.loggerFactory, this.asyncProvider);
            finalizedBlockRepo.LoadFinalizedBlockInfoAsync(this.network).GetAwaiter().GetResult();

            this.resultExecutorMock = new Mock<IPollResultExecutor>();

            this.votingManager = new VotingManager(this.federationManager, this.loggerFactory, this.slotsManager, this.resultExecutorMock.Object, new NodeStats(timeProvider, this.loggerFactory),
                 dataFolder, this.DataStoreSerializer, this.signals, finalizedBlockRepo);

            this.votingManager.Initialize();

            this.chainState = new ChainState();

            this.rulesEngine = new PoAConsensusRuleEngine(this.network, this.loggerFactory, new DateTimeProvider(), this.ChainIndexer, new NodeDeployments(this.network, this.ChainIndexer),
                this.consensusSettings, new Checkpoints(this.network, this.consensusSettings), new Mock<ICoinView>().Object, this.chainState, new InvalidBlockHashStore(timeProvider),
                new NodeStats(timeProvider, this.loggerFactory), this.slotsManager, this.poaHeaderValidator, this.votingManager, this.federationManager, this.asyncProvider, new ConsensusRulesContainer());

            List<ChainedHeader> headers = ChainedHeadersHelper.CreateConsecutiveHeaders(50, null, false, null, this.network);

            this.currentHeader = headers.Last();
        }

        public static IFederationManager CreateFederationManager(object caller, Network network, LoggerFactory loggerFactory, ISignals signals)
        {
            string dir = TestBase.CreateTestDir(caller);
            var keyValueRepo = new LevelDbKeyValueRepository(dir, new DataStoreSerializer(network.Consensus.ConsensusFactory));

            var settings = new NodeSettings(network, args: new string[] { $"-datadir={dir}" });
            var federationManager = new FederationManager(settings, network, loggerFactory, keyValueRepo, signals);
            federationManager.Initialize();

            return federationManager;
        }

        public static IFederationManager CreateFederationManager(object caller)
        {
            return CreateFederationManager(caller, new TestPoANetwork(), new ExtendedLoggerFactory(), new Signals.Signals(new LoggerFactory(), null));
        }

        public void InitRule(ConsensusRuleBase rule)
        {
            rule.Parent = this.rulesEngine;
            rule.Logger = this.loggerFactory.CreateLogger(rule.GetType().FullName);
            rule.Initialize();
        }
    }

    public class TestPoANetwork : PoANetwork
    {
        public TestPoANetwork(List<PubKey> pubKeysOverride = null)
        {
            List<IFederationMember> genesisFederationMembers;

            if (pubKeysOverride != null)
            {
                genesisFederationMembers = new List<IFederationMember>();

                foreach (PubKey key in pubKeysOverride)
                    genesisFederationMembers.Add(new FederationMember(key));
            }
            else
            {
                genesisFederationMembers = new List<IFederationMember>()
                {
                    new FederationMember(new PubKey("02d485fc5ae101c2780ff5e1f0cb92dd907053266f7cf3388eb22c5a4bd266ca2e")),
                    new FederationMember(new PubKey("026ed3f57de73956219b85ef1e91b3b93719e2645f6e804da4b3d1556b44a477ef")),
                    new FederationMember(new PubKey("03895a5ba998896e688b7d46dd424809b0362d61914e1432e265d9539fe0c3cac0")),
                    new FederationMember(new PubKey("020fc3b6ac4128482268d96f3bd911d0d0bf8677b808eaacd39ecdcec3af66db34")),
                    new FederationMember(new PubKey("038d196fc2e60d6dfc533c6a905ba1f9092309762d8ebde4407d209e37a820e462")),
                    new FederationMember(new PubKey("0358711f76435a508d98a9dee2a7e160fed5b214d97e65ea442f8f1265d09e6b55"))
                };
            }

            var baseOptions = this.Consensus.Options as PoAConsensusOptions;

            this.Consensus.Options = new PoAConsensusOptions
            {
                MaxBlockBaseSize = baseOptions.MaxBlockBaseSize,
                MaxStandardVersion = baseOptions.MaxStandardVersion,
                MaxStandardTxWeight = baseOptions.MaxStandardTxWeight,
                MaxBlockSigopsCost = baseOptions.MaxBlockSigopsCost,
                MaxStandardTxSigopsCost = baseOptions.MaxStandardTxSigopsCost,
                GenesisFederationMembers = genesisFederationMembers,
                TargetSpacingSeconds = 60,
                VotingEnabled = baseOptions.VotingEnabled,
                AutoKickIdleMembers = baseOptions.AutoKickIdleMembers,
                FederationMemberMaxIdleTimeSeconds = baseOptions.FederationMemberMaxIdleTimeSeconds
            };

            this.Consensus.SetPrivatePropertyValue(nameof(this.Consensus.MaxReorgLength), (uint)5);
        }
    }
}