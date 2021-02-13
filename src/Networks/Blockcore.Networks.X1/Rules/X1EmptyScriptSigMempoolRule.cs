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
    public class X1EmptyScriptSigMempoolRule : MempoolRule
    {
        public X1EmptyScriptSigMempoolRule(Network network,
            ITxMempool mempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            IConsensusRuleEngine consensusRules,
            ILoggerFactory loggerFactory) : base(network, mempool, mempoolSettings, chainIndexer, loggerFactory)
        {
        }

        public override void CheckTransaction(MempoolValidationContext context)
        {
            if (context.Transaction.IsCoinBase)
                return;

            foreach (var txin in context.Transaction.Inputs)
            {
                // According to BIP-0141, P2WPKH and P2WSH transaction must have an empty ScriptSig,
                // which is what we require to let a tx pass. The requirement's scope includes
                // Coinstake transactions as well as standard transactions.
                if ((txin.ScriptSig == null || txin.ScriptSig.Length == 0) && context.Transaction.HasWitness)
                    continue;

                this.logger.LogTrace($"(-)[FAIL_{nameof(X1EmptyScriptSigMempoolRule)}]".ToUpperInvariant());
                X1ConsensusErrors.ScriptSigNotEmpty.Throw();
            }
        }
    }
}