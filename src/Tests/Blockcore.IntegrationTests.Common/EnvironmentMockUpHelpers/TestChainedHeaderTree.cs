using System.Collections.Generic;
using System.Reflection;
using Blockcore.Base;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.Validators;
using Blockcore.Networks;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers
{
    /// <summary>Test-only implementation of <see cref="TestChainedHeaderTree"/> that exposes inner structures.</summary>
    internal class TestChainedHeaderTree : ChainedHeaderTree
    {
        public TestChainedHeaderTree(
            Network network,
            ILoggerFactory loggerFactory,
            IHeaderValidator headerValidator,
            ICheckpoints checkpoints,
            IChainState chainState,
            IFinalizedBlockInfoRepository finalizedBlockInfo,
            ConsensusSettings consensusSettings,
            IInvalidBlockHashStore invalidHashesStore) : base(network, loggerFactory, headerValidator, checkpoints,
                chainState, finalizedBlockInfo, consensusSettings, invalidHashesStore)
        {
        }

        public Dictionary<uint256, HashSet<int>> PeerIdsByTipHash => typeof(ChainedHeaderTree).GetField("peerIdsByTipHash", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this) as Dictionary<uint256, HashSet<int>>;

        public Dictionary<int, uint256> PeerTipsByPeerId => typeof(ChainedHeaderTree).GetField("peerTipsByPeerId", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this) as Dictionary<int, uint256>;

        public Dictionary<uint256, ChainedHeader> ChainedHeadersByHash => typeof(ChainedHeaderTree).GetField("chainedHeadersByHash", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this) as Dictionary<uint256, ChainedHeader>;
    }
}