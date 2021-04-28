using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.Xds.Rules
{
    /// <summary>
    /// Checks weather the transaction has witness.
    /// </summary>
    public class XdsRequireWitnessMempoolRule : MempoolRule
    {
        public XdsRequireWitnessMempoolRule(Network network,
            ITxMempool mempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            IConsensusRuleEngine consensusRules,
            ILoggerFactory loggerFactory) : base(network, mempool, mempoolSettings, chainIndexer, loggerFactory)
        {
        }

        public override void CheckTransaction(MempoolValidationContext context)
        {
            if (!context.Transaction.HasWitness)
            {
                this.logger.LogTrace($"(-)[FAIL_{nameof(XdsRequireWitnessMempoolRule)}]".ToUpperInvariant());
                XdsConsensusErrors.MissingWitness.Throw();
            }
        }
    }
}