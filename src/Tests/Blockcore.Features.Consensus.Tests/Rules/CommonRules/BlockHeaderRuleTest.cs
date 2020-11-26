using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Consensus.Tests.Rules.CommonRules
{
    public class BlockHeaderRuleTest
    {
        private readonly Network network;

        public BlockHeaderRuleTest()
        {
            this.network = KnownNetworks.RegTest;
        }

        [Fact]
        public async Task BlockReceived_IsNextBlock_ValidationSucessAsync()
        {
            TestRulesContext testContext = TestRulesContextFactory.CreateAsync(this.network);
            var blockHeaderRule = testContext.CreateRule<SetActivationDeploymentsPartialValidationRule>();

            var context = new PowRuleContext(new ValidationContext(), testContext.DateTimeProvider.GetTimeOffset());
            context.ValidationContext.BlockToValidate = KnownNetworks.RegTest.Consensus.ConsensusFactory.CreateBlock();
            context.ValidationContext.BlockToValidate.Header.HashPrevBlock = testContext.ChainIndexer.Tip.HashBlock;
            context.ValidationContext.ChainedHeaderToValidate = new ChainedHeader(context.ValidationContext.BlockToValidate.Header, context.ValidationContext.BlockToValidate.Header.GetHash(), 0);

            await blockHeaderRule.RunAsync(context);

            Assert.NotNull(context.ValidationContext.ChainedHeaderToValidate);
            Assert.NotNull(context.Flags);
        }
    }
}
