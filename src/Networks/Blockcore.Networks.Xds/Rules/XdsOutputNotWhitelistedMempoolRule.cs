using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.Xds.Rules
{
    /// <summary>
    /// Checks if transactions match the white-listing criteria. This rule and <see cref="XdsOutputNotWhitelistedRule"/> must correspond.
    /// </summary>
    public class XdsOutputNotWhitelistedMempoolRule : MempoolRule
    {
        public XdsOutputNotWhitelistedMempoolRule(Network network,
            ITxMempool mempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            IConsensusRuleEngine consensusRules,
            ILoggerFactory loggerFactory) : base(network, mempool, mempoolSettings, chainIndexer, loggerFactory)
        {
        }

        public override void CheckTransaction(MempoolValidationContext context)
        {
            if (context.Transaction.IsCoinStake || (context.Transaction.IsCoinBase && context.Transaction.Outputs[0].IsEmpty)) // also check the coinbase tx in PoW blocks
                return;

            foreach (var output in context.Transaction.Outputs)
            {
                if (XdsOutputNotWhitelistedRule.IsOutputWhitelisted(output))
                    continue;

                this.logger.LogTrace($"(-)[FAIL_{nameof(XdsOutputNotWhitelistedMempoolRule)}]".ToUpperInvariant());
                context.State.Fail(new MempoolError(XdsConsensusErrors.OutputNotWhitelisted)).Throw();
            }
        }
    }
}