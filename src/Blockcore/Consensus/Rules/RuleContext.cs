using System;
using Blockcore.Base.Deployments;
using Blockcore.Utilities;

namespace Blockcore.Consensus.Rules
{
    /// <summary>
    /// Context that contains variety of information regarding blocks validation and execution.
    /// </summary>
    public class RuleContext
    {
        public DateTimeOffset Time { get; set; }

        public ValidationContext ValidationContext { get; set; }

        public DeploymentFlags Flags { get; set; }

        /// <summary>Whether to skip block validation for this block due to either a checkpoint or assumevalid hash set.</summary>
        public bool SkipValidation { get; set; }

        public RuleContext()
        {
        }

        public RuleContext(ValidationContext validationContext, DateTimeOffset time) : base()
        {
            Guard.NotNull(validationContext, nameof(validationContext));

            this.ValidationContext = validationContext;
            this.Time = time;
        }
    }
}