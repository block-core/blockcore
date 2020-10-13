using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.Rules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus.Rules.ProvenHeaderRules
{
    /// <summary>
    /// Base rule to be used by all proven header validation rules.
    /// </summary>
    /// <remarks>
    /// We assume that in case normal headers are provided instead of proven headers we should ignore validation.
    /// This should be allowed by the behaviors only for whitelisted nodes.</remarks>
    /// <seealso cref="HeaderValidationConsensusRule" />
    public abstract class ProvenHeaderRuleBase : HeaderValidationConsensusRule
    {
        /// <summary>Allow access to the POS parent.</summary>
        protected PosConsensusRuleEngine PosParent;

        protected int LastCheckpointHeight;
        protected CheckpointInfo LastCheckpoint;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.PosParent = this.Parent as PosConsensusRuleEngine;

            Guard.NotNull(this.PosParent, nameof(this.PosParent));

            this.LastCheckpointHeight = this.Parent.Checkpoints.LastCheckpointHeight;
            this.LastCheckpoint = this.Parent.Checkpoints.GetCheckpoint(this.LastCheckpointHeight);
        }

        /// <summary>
        /// Determines whether proven headers are activated based on the proven header activation height and applicable network.
        /// </summary>
        /// <param name="height">The height of the header.</param>
        /// <returns>
        /// <c>true</c> if proven header height is past the activation height for the corresponding network;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool IsProvenHeaderActivated(int height)
        {
            return height > this.LastCheckpointHeight;
        }

        /// <inheritdoc/>
        public override void Run(RuleContext context)
        {
            if (context.SkipValidation)
            {
                this.Logger.LogTrace("(-)[VALIDATION_SKIPPED]");
                return;
            }

            Guard.NotNull(context.ValidationContext.ChainedHeaderToValidate, nameof(context.ValidationContext.ChainedHeaderToValidate));

            ChainedHeader chainedHeader = context.ValidationContext.ChainedHeaderToValidate;

            if (!this.IsProvenHeaderActivated(chainedHeader.Height))
            {
                this.Logger.LogTrace("(-)[PH_NOT_ACTIVATED]");
                return;
            }

            if (chainedHeader.ProvenBlockHeader == null)
            {
                // We skip validation if the header is a regular header
                // This is to allow white-listed peers to sync using regular headers.
                this.Logger.LogTrace("(-)[NOT_A_PROVEN_HEADER]");
                return;
            }

            this.ProcessRule((PosRuleContext)context, chainedHeader, chainedHeader.ProvenBlockHeader);
        }

        /// <summary>
        /// Processes the rule.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="chainedHeader">The chained header to be validated.</param>
        /// <param name="header">The Proven Header to be validated.</param>
        protected abstract void ProcessRule(PosRuleContext context, ChainedHeader chainedHeader, ProvenBlockHeader header);
    }
}