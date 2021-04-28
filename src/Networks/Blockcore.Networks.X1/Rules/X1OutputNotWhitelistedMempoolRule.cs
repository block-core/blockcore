using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.X1.Rules
{
    /// <summary>
    /// Checks if transactions match the white-listing criteria. This rule and <see cref="X1OutputNotWhitelistedRule"/> must correspond.
    /// </summary>
    public class X1OutputNotWhitelistedMempoolRule : MempoolRule
    {
        public X1OutputNotWhitelistedMempoolRule(Network network,
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
                if (X1OutputNotWhitelistedRule.IsOutputWhitelisted(output))
                    continue;

                this.logger.LogTrace($"(-)[FAIL_{nameof(X1OutputNotWhitelistedMempoolRule)}]".ToUpperInvariant());
                context.State.Fail(new MempoolError(X1ConsensusErrors.OutputNotWhitelisted)).Throw();
            }
        }
    }
}