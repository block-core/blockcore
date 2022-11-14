using System.Collections.Generic;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Networks.Strax.ScriptTemplates;

namespace Blockcore.Networks.Strax.Policies
{
    /// <summary>
    /// Strax-specific standard transaction definitions.
    /// </summary>
    public class StraxStandardScriptsRegistry : StandardScriptsRegistry
    {
        public const int MaxOpReturnRelay = 83;

        // Need a network-specific version of the template list
        private static readonly List<ScriptTemplate> standardTemplates = new List<ScriptTemplate>
        {
            new PayToPubkeyHashTemplate(),
            new PayToPubkeyTemplate(),
            new PayToScriptHashTemplate(),
            new PayToMultiSigTemplate(),
            new PayToFederationTemplate(),
            new TxNullDataTemplate(MaxOpReturnRelay),
            new PayToWitTemplate()
        };

        public override List<ScriptTemplate> GetScriptTemplates => standardTemplates;
    }
}
