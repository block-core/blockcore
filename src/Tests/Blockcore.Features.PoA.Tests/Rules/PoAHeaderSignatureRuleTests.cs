using System;
using System.Collections.Generic;
using Blockcore.Configuration;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Rules;
using Blockcore.Features.PoA.BasePoAFeatureConsensusRules;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.PoA.Tests.Rules
{
    public class PoAHeaderSignatureRuleTests : PoATestsBase
    {
        private readonly PoAHeaderSignatureRule signatureRule;

        private static Key key = new KeyTool(new DataFolder(string.Empty)).GeneratePrivateKey();

        public PoAHeaderSignatureRuleTests() : base(new TestPoANetwork(new List<PubKey>() { key.PubKey }))
        {
            this.signatureRule = new PoAHeaderSignatureRule();
            this.InitRule(this.signatureRule);
        }

        [Fact]
        public void SignatureIsValidated()
        {
            var validationContext = new ValidationContext() { ChainedHeaderToValidate = this.currentHeader };
            var ruleContext = new RuleContext(validationContext, DateTimeOffset.Now);

            Key randomKey = new KeyTool(new DataFolder(string.Empty)).GeneratePrivateKey();
            this.poaHeaderValidator.Sign(randomKey, this.currentHeader.Header as PoABlockHeader);

            this.chainState.ConsensusTip = new ChainedHeader(this.network.GetGenesis().Header, this.network.GetGenesis().GetHash(), 0);

            Assert.Throws<ConsensusErrorException>(() => this.signatureRule.Run(ruleContext));

            this.poaHeaderValidator.Sign(key, this.currentHeader.Header as PoABlockHeader);

            this.signatureRule.Run(ruleContext);
        }
    }
}
