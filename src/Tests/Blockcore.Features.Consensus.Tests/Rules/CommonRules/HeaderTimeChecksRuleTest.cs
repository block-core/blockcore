using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Consensus.Tests.Rules.CommonRules
{
    public class HeaderTimeChecksRuleTest
    {
        private readonly Network network;

        public HeaderTimeChecksRuleTest()
        {
            this.network = KnownNetworks.RegTest;
        }

        [Fact]
        public void ChecBlockPreviousTimestamp_ValidationFail()
        {
            TestRulesContext testContext = TestRulesContextFactory.CreateAsync(this.network);
            var rule = testContext.CreateRule<HeaderTimeChecksRule>();

            RuleContext context = new PowRuleContext(new ValidationContext(), testContext.DateTimeProvider.GetTimeOffset());
            context.ValidationContext.BlockToValidate = TestRulesContextFactory.MineBlock(KnownNetworks.RegTest, testContext.ChainIndexer);
            context.ValidationContext.ChainedHeaderToValidate = new ChainedHeader(context.ValidationContext.BlockToValidate.Header, context.ValidationContext.BlockToValidate.Header.GetHash(), testContext.ChainIndexer.Tip);
            context.Time = DateTimeProvider.Default.GetTimeOffset();

            // increment the bits.
            context.ValidationContext.BlockToValidate.Header.BlockTime = testContext.ChainIndexer.Tip.Header.BlockTime.AddSeconds(-1);

            ConsensusErrorException error = Assert.Throws<ConsensusErrorException>(() => rule.Run(context));
            Assert.Equal(ConsensusErrors.TimeTooOld, error.ConsensusError);
        }

        [Fact]
        public void ChecBlockFutureTimestamp_ValidationFail()
        {
            TestRulesContext testContext = TestRulesContextFactory.CreateAsync(this.network);
            var rule = testContext.CreateRule<HeaderTimeChecksRule>();

            RuleContext context = new PowRuleContext(new ValidationContext(), testContext.DateTimeProvider.GetTimeOffset());
            context.ValidationContext.BlockToValidate = TestRulesContextFactory.MineBlock(KnownNetworks.RegTest, testContext.ChainIndexer);
            context.ValidationContext.ChainedHeaderToValidate = new ChainedHeader(context.ValidationContext.BlockToValidate.Header, context.ValidationContext.BlockToValidate.Header.GetHash(), testContext.ChainIndexer.Tip);
            context.Time = DateTimeProvider.Default.GetTimeOffset();

            // increment the bits.
            context.ValidationContext.BlockToValidate.Header.BlockTime = context.Time.AddHours(3);

            ConsensusErrorException error = Assert.Throws<ConsensusErrorException>(() => rule.Run(context));
            Assert.Equal(ConsensusErrors.TimeTooNew, error.ConsensusError);
        }
    }
}
