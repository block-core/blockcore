﻿using System.Linq;
using System.Threading.Tasks;
using Blockcore.Base.Deployments;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.Rules.UtxosetRules;

namespace Blockcore.Networks.Strax.Rules
{
    /// <summary>
    /// This rule performs further coldstaking transaction validation when cold staking balances are spent with a
    /// cold staking "hotPubKey" inside of a cold staking transaction. The cold staking "hotPubKey" is the pubKey
    /// that allows spending only to the same address.
    /// </summary>
    /// <remarks><para>
    /// This code will perform further validation for transactions that spend the new scriptPubKey containing the
    /// new <see cref="OpcodeType.OP_CHECKCOLDSTAKEVERIFY"/> opcode using a hotPubKeyHash inside of a coinstake
    /// transaction. Those are the conditions under which the <see cref="PosTransaction.IsColdCoinStake"/> flag will
    /// be set and it is therefore the flag we use to determine if these rules should be applied. Due to a check
    /// being done inside of the implementation of the <see cref="OpcodeType.OP_CHECKCOLDSTAKEVERIFY"/> opcode this
    /// flag can only be set for coinstake transactions after the opcode implementation has been activated (at a
    /// specified block height). The opcode activation flag is <see cref="ScriptVerify.CheckColdStakeVerify"/> and
    /// it is set in <see cref="DeploymentFlags.ScriptFlags"/> when the block height is greater than or equal to <see
    /// cref="PosConsensusOptions.ColdStakingActivationHeight"/>.
    /// </para><para>
    /// The following conditions are enforced for cold staking transactions. This rule implements all but the first one:
    /// <list type="number">
    /// <item>Check if the transaction spending an output, which contains this instruction, is a coinstake transaction.
    /// If it is not, the script fails. This has already been checked within the opcode implementation before setting
    /// <see cref="PosTransaction.IsColdCoinStake"/> so it will not be checked here.</item>
    /// <item>Check that ScriptPubKeys of all inputs of this transaction are the same. If they are not, the script
    /// fails.</item>
    /// <item>Check that ScriptPubKeys of all outputs of this transaction, except for the marker output (a special
    /// first output of each coinstake transaction) and the pubkey output (an optional special second output that
    /// contains public key in coinstake transaction), are the same as ScriptPubKeys of the inputs. If they are not,
    /// the script fails.</item>
    /// <item>Check that the sum of values of all inputs is smaller or equal to the sum of values of all outputs. If
    /// this does not hold, the script fails.</item>
    /// </list></para></remarks>
    public class StraxColdStakingRule : UtxoStoreConsensusRule
    {
        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.BadColdstakeInputs">Thrown if the input scriptPubKeys mismatch.</exception>
        /// <exception cref="ConsensusErrors.BadColdstakeOutputs">Thrown if the output scriptPubKeys mismatch.</exception>
        /// <exception cref="ConsensusErrors.BadColdstakeAmount">Thrown if the total input is smaller or equal than the sum of outputs.</exception>
        public override Task RunAsync(RuleContext context)
        {
            // Get the second transaction so that we can confirm whether it is a cold coin stake transaction.
            Block block = context.ValidationContext.BlockToValidate;
            PosTransaction coinstakeTransaction = ((block.Transactions.Count >= 2) ? block.Transactions[1] : null) as PosTransaction;

            // If there is no coinstake transaction or it is not a cold coin stake transaction then this rule is not required.
            // The "IsColdCoinStake" flag will only be set in the OP_CHECKCOLDSTAKEVERIFY if ScriptFlags in DeploymentFlags has "CheckColdStakeVerify" set.
            if (!(coinstakeTransaction?.IsColdCoinStake ?? false))
            {
                this.Logger.LogTrace("(-)[SKIP_COLDSTAKE_RULE]");
                return Task.CompletedTask;
            }

            var posRuleContext = context as PosRuleContext;

            // Verify that all inputs map to incoming outputs.
            if (coinstakeTransaction.Inputs.Any(i => !posRuleContext.CoinStakePrevOutputs.ContainsKey(i)))
            {
                this.Logger.LogTrace("(-)[COLDSTAKE_INPUTS_WITHOUT_OUTPUTS]");
                ConsensusErrors.BadColdstakeInputs.Throw();
            }

            // Check that ScriptPubKeys of all inputs of this transaction are the same. If they are not, the script fails.
            // Due to this being a coinstake transaction we know it will have at least one input.
            Script scriptPubKey = posRuleContext.CoinStakePrevOutputs[coinstakeTransaction.Inputs[0]].ScriptPubKey;
            for (int i = 1; i < coinstakeTransaction.Inputs.Count; i++)
            {
                if (scriptPubKey != posRuleContext.CoinStakePrevOutputs[coinstakeTransaction.Inputs[i]]?.ScriptPubKey)
                {
                    this.Logger.LogTrace("(-)[BAD_COLDSTAKE_INPUTS]");
                    ConsensusErrors.BadColdstakeInputs.Throw();
                }
            }

            // Check that the second output is a special output for presenting the public key with an OP_RETURN and that
            // the output value is zero. Checking for the OP_RETURN ensures that the PosBlockSignatureRule won't match the
            // PayToPubKey template. This will ensure that an attacker won't use a PayToPubKey output here to spend our
            // cold staking balance (using the hot wallet key) to an address other then our special scriptpubkey.
            if ((coinstakeTransaction.Outputs[1].ScriptPubKey.ToOps().FirstOrDefault()?.Code != OpcodeType.OP_RETURN) ||
                (coinstakeTransaction.Outputs[1].Value != Money.Zero))
            {
                this.Logger.LogTrace("(-)[MISSING_COLDSTAKE_PUBKEY_OUTPUT]");
                ConsensusErrors.BadColdstakeOutputs.Throw();
            }

            // Check that ScriptPubKeys of all outputs of this transaction, except for the marker output (a special first
            // output of each coinstake transaction) and the pubkey output (an optional special second output that contains
            // public key in coinstake transaction), are the same as ScriptPubKeys of the inputs. If they are not, the script fails.
            bool cirrusScriptFlagSeen = false;

            for (int i = 2; i < coinstakeTransaction.Outputs.Count; i++)
            {
                if (scriptPubKey != coinstakeTransaction.Outputs[i].ScriptPubKey)
                {
                    // We have to make allowance for the fact that one of the cold stake outputs will be the Cirrus reward payment script.
                    // So do not throw a consensus error for those unless more than one appears in the transaction.
                    // We do not need to check the reward amount, because that is done elsewhere.
                    if (coinstakeTransaction.Outputs[i].ScriptPubKey == StraxCoinstakeRule.CirrusRewardScript && !cirrusScriptFlagSeen)
                    {
                        cirrusScriptFlagSeen = true;
                        continue;
                    }

                    this.Logger.LogTrace("(-)[BAD_COLDSTAKE_OUTPUTS]");
                    ConsensusErrors.BadColdstakeOutputs.Throw();
                }
            }
            
            // Check that the sum of values of all inputs is smaller or equal to the sum of values of all outputs. If this does
            // not hold, the script fails. This prevents the hot balance from being reduced.
            if (posRuleContext.TotalCoinStakeValueIn > coinstakeTransaction.TotalOut)
            {
                this.Logger.LogTrace("(-)[COLDSTAKE_INPUTS_EXCEED_OUTPUTS]");
                ConsensusErrors.BadColdstakeAmount.Throw();
            }

            this.Logger.LogTrace("(-)");

            return Task.CompletedTask;
        }
    }
}
