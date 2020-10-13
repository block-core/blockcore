using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.CommonRules
{
    /// <summary>Context checks on a POS block.</summary>
    public class PosCoinstakeRule : PartialValidationConsensusRule
    {
        /// <summary>Allow access to the POS parent.</summary>
        protected PosConsensusRuleEngine PosParent;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.PosParent = this.Parent as PosConsensusRuleEngine;

            Guard.NotNull(this.PosParent, nameof(this.PosParent));
        }

        /// <inheritdoc />
        /// <exception cref="ConsensusErrors.BadStakeBlock">The coinbase output (first transaction) is not empty.</exception>
        /// <exception cref="ConsensusErrors.BadStakeBlock">The second transaction is not a coinstake transaction.</exception>
        /// <exception cref="ConsensusErrors.BadMultipleCoinstake">There are multiple coinstake tranasctions in the block.</exception>
        /// <exception cref="ConsensusErrors.BlockTimeBeforeTrx">The block contains a transaction with a timestamp after the block timestamp.</exception>
        public override Task RunAsync(RuleContext context)
        {
            if (context.SkipValidation)
                return Task.CompletedTask;

            Block block = context.ValidationContext.BlockToValidate;

            // Check if the block was produced using POS. 
            if (BlockStake.IsProofOfStake(block))
            {
                // Coinbase output should be empty if proof-of-stake block.
                if ((block.Transactions[0].Outputs.Count != 1) || (!block.Transactions[0].Outputs[0].IsEmpty))
                {
                    if (this.PosParent.Network.Consensus.PosEmptyCoinbase)
                    {
                        this.Logger.LogTrace("(-)[COINBASE_NOT_EMPTY]");
                        ConsensusErrors.BadStakeBlock.Throw();
                    }

                    // First output must be empty.
                    if ((!block.Transactions[0].Outputs[0].IsEmpty))
                    {
                        this.Logger.LogTrace("(-)[COINBASE_NOT_EMPTY]");
                        ConsensusErrors.BadStakeBlock.Throw();
                    }

                    // Check that the rest of the outputs are not spendable (op_return)
                    foreach (TxOut txOut in block.Transactions[0].Outputs.Skip(1))
                    {
                        // Only op_return are allowed in coinbase.
                        if (!txOut.ScriptPubKey.IsUnspendable)
                        {
                            this.Logger.LogTrace("(-)[COINBASE_SPENDABLE]");
                            ConsensusErrors.BadStakeBlock.Throw();
                        }
                    }
                }

                // Second transaction must be coinstake, the rest must not be.
                if (!block.Transactions[1].IsCoinStake)
                {
                    this.Logger.LogTrace("(-)[NO_COINSTAKE]");
                    ConsensusErrors.BadStakeBlock.Throw();
                }

                if (block.Transactions.Skip(2).Any(t => t.IsCoinStake))
                {
                    this.Logger.LogTrace("(-)[MULTIPLE_COINSTAKE]");
                    ConsensusErrors.BadMultipleCoinstake.Throw();
                }
            }

            // TODO: while time is not used anymore 
            // we keep this check to avoid a network split

            // Check transactions.
            foreach (Transaction transaction in block.Transactions)
            {
                // With the new changes this check is actually not needed amnymore.
                // The trx time has no meaning for the network,its a legacy field from
                // the days where coin age was used for POS protocol.
                if (transaction is IPosTransactionWithTime posTrx)
                {
                    // Check transaction timestamp.
                    if (block.Header.Time < posTrx.Time)
                    {
                        this.Logger.LogDebug("Block contains transaction with timestamp {0}, which is greater than block's timestamp {1}.", posTrx.Time, block.Header.Time);
                        this.Logger.LogTrace("(-)[TX_TIME_MISMATCH]");
                        ConsensusErrors.BlockTimeBeforeTrx.Throw();
                    }
                }
            }
            
            return Task.CompletedTask;
        }
    }
}