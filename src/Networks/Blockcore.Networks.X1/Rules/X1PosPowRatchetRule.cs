using System;
using System.Threading.Tasks;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.Consensus.Rules;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Networks.X1.Consensus;
using Microsoft.Extensions.Logging;

namespace Blockcore.Networks.X1.Rules
{
    public class X1PosPowRatchetRule : PartialValidationConsensusRule
    {
        private X1ConsensusOptions posConsensusOptions;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.posConsensusOptions = (this.Parent as PosConsensusRuleEngine)?.Network.Consensus.Options as X1ConsensusOptions;
            if(this.posConsensusOptions == null)
                throw new ArgumentNullException(nameof(this.posConsensusOptions));
        }

        public override Task RunAsync(RuleContext context)
        {
            if (context.SkipValidation)
                return Task.CompletedTask;

            // Check consistency of ChainedHeader height and the height written in the coinbase tx
            var newHeight = GetHeightOfBlockToValidateSafe(context);
            
            // Get the algorithm of the block we are looking at
            bool isProofOfStake = BlockStake.IsProofOfStake(context.ValidationContext.BlockToValidate);

            // Check if there is a rule active, and if so, check if the algorithm is allowed at this height
            if (this.posConsensusOptions.IsAlgorithmAllowed(isProofOfStake, newHeight))
            {
                // yes, rule passed
                return Task.CompletedTask;
            }
               
            // no, this block is not acceptable
            this.Logger.LogTrace("(-)[BAD-POS-POW-RATCHET-SEQUENCE]");
            X1ConsensusErrors.BadPosPowRatchetSequence.Throw();

            return Task.CompletedTask;
        }

        /// <summary>
        /// From <see cref="CoinbaseHeightRule"/>. Very safe way to determine the true
        /// height of the block being checked.
        /// </summary>
        /// <returns>The height in the chain of the block being checked.</returns>
        int GetHeightOfBlockToValidateSafe(RuleContext context)
        {
            int newHeight = context.ValidationContext.ChainedHeaderToValidate.Height;
            Block block = context.ValidationContext.BlockToValidate;

            var expect = new Script(Op.GetPushOp(newHeight));
            Script actual = block.Transactions[0].Inputs[0].ScriptSig;
            if (!this.StartWith(actual.ToBytes(true), expect.ToBytes(true)))
            {
                this.Logger.LogTrace("(-)[BAD_COINBASE_HEIGHT]");
                ConsensusErrors.BadCoinbaseHeight.Throw();
            }

            return newHeight;
        }

        /// <summary>
        /// Checks if first <paramref name="subset.Lenght"/> entries are equal between two arrays.
        /// </summary>
        /// <param name="bytes">Main array.</param>
        /// <param name="subset">Subset array.</param>
        /// <returns><c>true</c> if <paramref name="subset.Lenght"/> entries are equal between two arrays. Otherwise <c>false</c>.</returns>
        private bool StartWith(byte[] bytes, byte[] subset)
        {
            if (bytes.Length < subset.Length)
                return false;

            for (int i = 0; i < subset.Length; i++)
            {
                if (subset[i] != bytes[i])
                    return false;
            }

            return true;
        }
    }
}
