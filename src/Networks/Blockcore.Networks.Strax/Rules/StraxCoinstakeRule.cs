using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.Rules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.Strax.Rules
{
    /// <summary>Context checks on a Strax POS block.</summary>
    public class StraxCoinstakeRule : PartialValidationConsensusRule
    {
        // Check that at least one of the coinstake outputs goes to the reward scriptPubKey.
        // The actual percentage of the reward sent to this script is checked within the coinview rule.
        // This is an anyone-can-spend scriptPubKey as no signature is required for the redeem script to be valid.
        // Recall that a scriptPubKey is not network-specific, only the address format that it translates into would depend on the version bytes etc. defined by the network.

        // The redeem script is defined first (and separately) because it is needed for claiming the reward.
        // It is not the scriptPubKey that must appear in the reward transaction output.
        public static readonly Script CirrusRewardScriptRedeem = new Script(new List<Op>() { OpcodeType.OP_TRUE });

        // This payment script is what must actually be checked against in the consensus rule i.e. the reward transaction has this as an output's scriptPubKey.
        public static readonly Script CirrusRewardScript = CirrusRewardScriptRedeem.PaymentScript;

        /// <summary>Allow access to the POS parent.</summary>
        protected PosConsensusRuleEngine PosParent;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.PosParent = this.Parent as PosConsensusRuleEngine;

            Guard.NotNull(this.PosParent, nameof(this.PosParent));
        }

        // This is not used within consensus, but it makes sense to keep the value close to the other script definitions so that it isn't buried inside the reward claimer.
        public static Script CirrusTransactionTag(string dummyAddress)
        {
            if (string.IsNullOrEmpty(dummyAddress))
                return null;

            return new Script(OpcodeType.OP_RETURN, Op.GetPushOp(Encoding.UTF8.GetBytes(dummyAddress)));
        }

        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.BadStakeBlock">The coinbase output (first transaction) is not empty.</exception>
        /// <exception cref="ConsensusErrors.BadStakeBlock">The second transaction is not a coinstake transaction.</exception>
        /// <exception cref="ConsensusErrors.BadMultipleCoinstake">There are multiple coinstake tranasctions in the block.</exception>
        public override Task RunAsync(RuleContext context)
        {
            if (context.SkipValidation)
                return Task.CompletedTask;

            Block block = context.ValidationContext.BlockToValidate;

            if (BlockStake.IsProofOfStake(block))
            {
                Transaction coinBase = block.Transactions[0];

                // On the Stratis network, we mandated that the coinbase output must be empty if the block is proof-of-stake.
                // Here, we anticipate that the coinbase will contain the segwit witness commitment.
                // For maximum flexibility in the future we don't want to restrict what else the coinbase in a PoS block can contain, with some limitations:
                // 1. No outputs should be spendable (we could mandate that the PoS reward must be wholly contained in the coinstake, but it is sufficient that the coinbase outputs are unspendable)
                // 2. The first coinbase output must be empty

                // First output must be empty.
                if ((!coinBase.Outputs[0].IsEmpty))
                {
                    this.Logger.LogTrace("(-)[COINBASE_NOT_EMPTY]");
                    ConsensusErrors.BadStakeBlock.Throw();
                }

                // Check that the rest of the outputs are not spendable (OP_RETURN)
                foreach (TxOut txOut in coinBase.Outputs.Skip(1))
                {
                    // Only OP_RETURN scripts are allowed in coinbase.
                    if (!txOut.ScriptPubKey.IsUnspendable)
                    {
                        this.Logger.LogTrace("(-)[COINBASE_SPENDABLE]");
                        ConsensusErrors.BadStakeBlock.Throw();
                    }
                }

                Transaction coinStake = block.Transactions[1];

                // Second transaction must be a coinstake, the rest must not be.
                if (!coinStake.IsCoinStake)
                {
                    this.Logger.LogTrace("(-)[NO_COINSTAKE]");
                    ConsensusErrors.BadStakeBlock.Throw();
                }

                bool cirrusRewardOutput = false;
                foreach (var output in coinStake.Outputs)
                {
                    if (output.ScriptPubKey == CirrusRewardScript)
                    {
                        cirrusRewardOutput = true;
                    }
                }

                if (!cirrusRewardOutput)
                {
                    this.Logger.LogTrace("(-)[MISSING_REWARD_SCRIPT_COINSTAKE_OUTPUT]");
                    ConsensusErrors.BadTransactionNoOutput.Throw();
                }

                if (block.Transactions.Skip(2).Any(t => t.IsCoinStake))
                {
                    this.Logger.LogTrace("(-)[MULTIPLE_COINSTAKE]");
                    ConsensusErrors.BadMultipleCoinstake.Throw();
                }
            }

            return Task.CompletedTask;
        }
    }
}
