using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.Rules;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.Xds.Rules
{
    /// <summary>
    /// Checks <see cref="XdsMain"/> transaction inputs have empty ScriptSig fields.
    /// </summary>
    public class XdsEmptyScriptSigRule : PartialValidationConsensusRule
    {
        public override Task RunAsync(RuleContext context)
        {
            var block = context.ValidationContext.BlockToValidate;

            foreach (var tx in block.Transactions)
            {
                if (tx.IsCoinBase)
                    continue;

                foreach (var txin in tx.Inputs)
                {
                    // According to BIP-0141, P2WPKH and P2WSH transaction must have an empty ScriptSig,
                    // which is what we require to let a tx pass. The requirement's scope includes
                    // Coinstake transactions as well as standard transactions.
                    if ((txin.ScriptSig == null || txin.ScriptSig.Length == 0) && tx.HasWitness)
                        continue;

                    this.Logger.LogTrace("(-)[SCRIPTSIG_NOT_EMPTY]");
                    new ConsensusError("scriptsig-not-empty", "SegWit requires empty ScriptSig fields.").Throw();
                }
            }

            return Task.CompletedTask;
        }
    }
}