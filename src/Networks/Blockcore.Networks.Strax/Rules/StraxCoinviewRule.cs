using System.Collections.Generic;
using System.Linq;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.Strax.Rules
{
    /// <summary>
    /// Strax PoS overrides for certain coinview rule checks.
    /// </summary>
    public sealed class StraxCoinviewRule : CheckPosUtxosetRule
    {
        // 50% of the block reward should be assigned to the reward script.
        // This has to be within the coinview rule because we need access to the coinstake input value to determine the size of the block reward.
        public static readonly int CirrusRewardPercentage = 50;

        /// <inheritdoc />
        public override void CheckBlockReward(RuleContext context, Money fees, int height, Block block)
        {
            // Currently this rule only applies to PoS blocks
            if (BlockStake.IsProofOfStake(block))
            {
                var posRuleContext = context as PosRuleContext;
                Transaction coinstake = block.Transactions[1];
                Money stakeReward = coinstake.TotalOut - posRuleContext.TotalCoinStakeValueIn;
                Money calcStakeReward = fees + GetProofOfStakeReward(height);

                this.Logger.LogDebug("Block stake reward is {0}, calculated reward is {1}.", stakeReward, calcStakeReward);
                if (stakeReward > calcStakeReward)
                {
                    this.Logger.LogTrace("(-)[BAD_COINSTAKE_AMOUNT]");
                    ConsensusErrors.BadCoinstakeAmount.Throw();
                }

                // Compute the total reward amount sent to the reward script.
                // We only mandate that at least x% of the reward is sent there, there are no other constraints on what gets done with the rest of the reward.
                Money rewardScriptTotal = Money.Coins(0.0m);

                foreach (TxOut output in coinstake.Outputs)
                {
                    if (output.ScriptPubKey == StraxCoinstakeRule.CirrusRewardScript)
                        rewardScriptTotal += output.Value;
                }

                // It must be CirrusRewardPercentage of the maximum possible reward precisely.
                // This additionally protects cold staking transactions from over-allocating to the Cirrus reward script at the expense of the non-Cirrus reward.
                // This means that the hot key can be used for staking by anybody and they will not be able to redirect the non-Cirrus reward to the Cirrus script.
                // It must additionally not be possible to short-change the Cirrus reward script by deliberately sacrificing part of the overall claimed reward.
                if ((calcStakeReward * CirrusRewardPercentage / 100) != rewardScriptTotal)
                {
                    this.Logger.LogTrace("(-)[BAD_COINSTAKE_REWARD_SCRIPT_AMOUNT]");
                    ConsensusErrors.BadTransactionScriptError.Throw();
                }
            }
            else
            {
                Money blockReward = fees + GetProofOfWorkReward(height);
                this.Logger.LogDebug("Block reward is {0}, calculated reward is {1}.", block.Transactions[0].TotalOut, blockReward);
                if (block.Transactions[0].TotalOut > blockReward)
                {
                    this.Logger.LogTrace("(-)[BAD_COINBASE_AMOUNT]");
                    ConsensusErrors.BadCoinbaseAmount.Throw();
                }
            }
        }

        protected override bool CheckInput(Transaction tx, int inputIndexCopy, TxOut txout, PrecomputedTransactionData txData, TxIn input, DeploymentFlags flags)
        {
            if (txout.ScriptPubKey.IsScriptType(ScriptType.P2SH))
            {
                // federation output is p2sh

                IList<Op> ops = input.ScriptSig.ToOps();
                if (ops.Count > 5 && ops[0].PushData.Length == 0) // first op is zero, federation is at least 3 participants, last is redeem script
                {
                    Script redeemScript = new Script(ops.Last().PushData);

                    foreach (Op innerOp in redeemScript.ToOps())
                    {
                        if (innerOp.Code == OpcodeType.OP_NOP9)
                        {
                            // federation overrides OP_NOP9 to push fed pub keys to the stack and the
                            // needed signatures to satisfy the federation multisig, to avoid changing
                            // the script engine for now we ignore such outputs and consider them valid
                            // (as was with the opcode OP_NOP9) a malicious node may cause blockcore nodes
                            // to accept invalid blocks however as long as blockcore nodes are minority nodes
                            // its an acceptable risk, users not part of a federation should not really be effected.

                            return true;
                        }
                    }
                }
            }

            AllowSpend(txout, tx);

            return base.CheckInput(tx, inputIndexCopy, txout, txData, input, flags);
        }

        private void AllowSpend(TxOut prevOut, Transaction tx)
        {
            // We further need to check that any transactions that spend outputs from the reward script only go to the cross-chain multisig.
            // This check is not isolated to PoS specifically.
            if (prevOut.ScriptPubKey == StraxCoinstakeRule.CirrusRewardScript)
            {
                foreach (TxOut output in tx.Outputs)
                {
                    // We allow OP_RETURNs for tagging purposes, but they must not be allowed to have any value attached
                    // (as that would then be burning Cirrus rewards)
                    if (output.ScriptPubKey.IsUnspendable)
                    {
                        if (output.Value != 0)
                        {
                            this.Logger.LogTrace("(-)[INVALID_REWARD_OP_RETURN_SPEND]");
                            ConsensusErrors.BadTransactionScriptError.Throw();
                        }

                        continue;
                    }

                    // Every other (spendable) output must go to the multisig
                    if (output.ScriptPubKey != ((StraxBaseNetwork)(this.Parent.Network)).Federations.GetOnlyFederation().MultisigScript.PaymentScript)
                    {
                        this.Logger.LogTrace("(-)[INVALID_REWARD_SPEND_DESTINATION]");
                        ConsensusErrors.BadTransactionScriptError.Throw();
                    }
                }

                this.Logger.LogDebug($"Reward distribution transaction validated in consensus, spending to '{prevOut.ScriptPubKey}'.");
            }

            // Otherwise allow the spend (do nothing).
        }

        protected override uint GetP2SHSignatureOperationsCount(Transaction transaction, UnspentOutputSet inputs)
        {
            if (transaction.IsCoinBase)
                return 0;

            uint sigOps = 0;
            for (int i = 0; i < transaction.Inputs.Count; i++)
            {
                TxOut prevout = inputs.GetOutputFor(transaction.Inputs[i]);
                if (prevout.ScriptPubKey.IsScriptType(ScriptType.P2SH))
                    sigOps += GetSigOpCount(prevout.ScriptPubKey, this.Parent.Network, transaction.Inputs[i].ScriptSig);
            }

            return sigOps;
        }

        private uint GetSigOpCount(Script script, Network network, Script scriptSig)
        {
            if (!script.IsScriptType(ScriptType.P2SH))
                return script.GetSigOpCount(true);
            // This is a pay-to-script-hash scriptPubKey;
            // get the last item that the scriptSig
            // pushes onto the stack:
            bool validSig = new PayToScriptHashTemplate().CheckScriptSig(network, scriptSig, script);
            return !validSig ? 0 : GetSigOpCount(new Script(scriptSig.ToOps().Last().PushData), true, network);
            // ... and return its opcount:
        }

        private uint GetSigOpCount(Script script, bool fAccurate, Network network = null)
        {
            if (network is not StraxBaseNetwork straxNetwork)
            {
                throw new System.InvalidCastException($"Expected type {nameof(StraxBaseNetwork)}");
            }

            uint n = 0;
            Op lastOpcode = null;
            foreach (Op op in script.ToOps())
            {
                if (op.Code == OpcodeType.OP_CHECKSIG || op.Code == OpcodeType.OP_CHECKSIGVERIFY)
                    n++;
                else if (op.Code == OpcodeType.OP_CHECKMULTISIG || op.Code == OpcodeType.OP_CHECKMULTISIGVERIFY)
                {
                    if (fAccurate && straxNetwork?.Federations != null && lastOpcode.Code == OpcodeType.OP_NOP9) // OpcodeType.OP_FEDERATION)
                        n += (uint)straxNetwork.Federations.GetOnlyFederation().GetFederationDetails().transactionSigningKeys.Length;
                    else if (fAccurate && lastOpcode != null && lastOpcode.Code >= OpcodeType.OP_1 && lastOpcode.Code <= OpcodeType.OP_16)
                        n += (lastOpcode.PushData == null || lastOpcode.PushData.Length == 0) ? 0U : lastOpcode.PushData[0];
                    else
                        n += 20;
                }
                lastOpcode = op;
            }
            return n;
        }
    }
}