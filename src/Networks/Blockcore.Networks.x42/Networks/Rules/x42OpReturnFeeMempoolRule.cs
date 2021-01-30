using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.MemoryPool.Rules;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.x42.Networks.Consensus.Rules
{
    /// <summary>
    /// Verify the OP_RETURN fee.
    /// </summary>
    public class x42OpReturnFeeMempoolRule : CheckFeeMempoolRule
    {
        public x42OpReturnFeeMempoolRule(Network network,
            ITxMempool mempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            ILoggerFactory loggerFactory) : base(network, mempool, mempoolSettings, chainIndexer, loggerFactory)
        {
        }

        public override void CheckTransaction(MempoolValidationContext context)
        {
            if (context.Transaction.IsCoinBase)
                return;

            List<byte[]> opReturns = context.Transaction.Outputs.Select(o => o.ScriptPubKey.ToBytes(true)).Where(b => IsOpReturn(b)).ToList();
            Money transactionFees = context.Fees;
            FeeRate OpReturnFeeRate = new FeeRate(((x42Consensus)this.network.Consensus).MinOpReturnFee);

            // If there is OP_RETURN data, we will want to make sure the fee is correct.
            if (opReturns.Count() > 0)
            {
                var opReturnSize = opReturns.Sum(r => r.Length);
                if (transactionFees < OpReturnFeeRate.GetFee(opReturnSize))
                {
                    this.logger.LogTrace($"(-)[FAIL_{nameof(x42OpReturnFeeMempoolRule)}]".ToUpperInvariant());
                    x42ConsensusErrors.InsufficientOpReturnFee.Throw();
                }
            }

            base.CheckTransaction(context);
        }

        public static bool IsOpReturn(byte[] bytes)
        {
            return bytes.Length > 0 && bytes[0] == (byte)OpcodeType.OP_RETURN;
        }
    }
}