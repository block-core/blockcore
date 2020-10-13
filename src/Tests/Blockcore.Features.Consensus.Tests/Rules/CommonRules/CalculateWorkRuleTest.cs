using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Tests.Common;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Consensus.Tests.Rules.CommonRules
{
    public class CalculateWorkRuleTest : TestConsensusRulesUnitTestBase
    {
        [Fact]
        public void Run_ProofOfWorkBlock_CheckPow_InValidPow_ThrowsHighHashConsensusErrorException()
        {
            Block block = this.network.CreateBlock();

            this.ChainIndexer.SetTip(ChainedHeadersHelper.CreateConsecutiveHeaders(10, this.ChainIndexer.Tip).Last());

            this.ruleContext.ValidationContext = new ValidationContext()
            {
                BlockToValidate = block,
                ChainedHeaderToValidate = this.ChainIndexer.GetHeader(4)
            };

            var exception = Assert.Throws<ConsensusErrorException>(() =>
                this.consensusRules.RegisterRule<CheckDifficultyPowRule>().Run(this.ruleContext));

            Assert.Equal(ConsensusErrors.HighHash, exception.ConsensusError);
        }

        [Fact]
        public void Run_ProofOfWorkBlock_CheckPow_InValidPow_ThrowsBadDiffBitsConsensusErrorException()
        {
            Block block = TestRulesContextFactory.MineBlock(KnownNetworks.RegTest, this.ChainIndexer);

            this.ruleContext.ValidationContext = new ValidationContext()
            {
                BlockToValidate = block,
                ChainedHeaderToValidate = new ChainedHeader(block.Header, block.Header.GetHash(), this.ChainIndexer.GetHeader(block.Header.HashPrevBlock))
            };

            var exception = Assert.Throws<ConsensusErrorException>(() =>
                this.consensusRules.RegisterRule<CheckDifficultyPowRule>().Run(this.ruleContext));

            Assert.Equal(ConsensusErrors.BadDiffBits, exception.ConsensusError);
        }
    }
}