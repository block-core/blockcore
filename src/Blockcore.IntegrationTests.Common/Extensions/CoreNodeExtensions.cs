using System.IO;
using System.Linq;
using NBitcoin;
using Blockcore.Consensus;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;

namespace Blockcore.IntegrationTests
{
    public static class CoreNodeExtensions
    {
        public static void AppendToConfig(this CoreNode node, string configKeyValueItem)
        {
            using (StreamWriter sw = File.AppendText(node.Config))
            {
                sw.WriteLine(configKeyValueItem);
            }
        }

        public static Money GetProofOfWorkRewardForMinedBlocks(this CoreNode node, int numberOfBlocks)
        {
            var coinviewRule = node.FullNode.NodeService<IConsensusRuleEngine>().GetRule<CoinViewRule>();

            int startBlock = node.FullNode.ChainIndexer.Height - numberOfBlocks + 1;

            return Enumerable.Range(startBlock, numberOfBlocks)
                .Sum(p => coinviewRule.GetProofOfWorkReward(p));
        }       
    }
}