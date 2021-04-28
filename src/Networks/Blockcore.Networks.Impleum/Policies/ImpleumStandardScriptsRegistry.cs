using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.ScriptInfo;

namespace Blockcore.Networks.Impleum.Policies
{
    /// <summary>
    /// Blockcore sample coin-specific standard transaction definitions.
    /// </summary>
    public class ImpleumStandardScriptsRegistry : StandardScriptsRegistry
    {
        // See MAX_OP_RETURN_RELAY in Bitcoin Core, <script/standard.h.>
        // 80 bytes of data, +1 for OP_RETURN, +2 for the pushdata opcodes.
        public const int MaxOpReturnRelay = 83;

        // Need a network-specific version of the template list
        private readonly List<ScriptTemplate> standardTemplates = new List<ScriptTemplate>
        {
            PayToPubkeyHashTemplate.Instance,
            PayToPubkeyTemplate.Instance,
            PayToScriptHashTemplate.Instance,
            PayToMultiSigTemplate.Instance,
            new TxNullDataTemplate(MaxOpReturnRelay),
            PayToWitTemplate.Instance
        };

        public override List<ScriptTemplate> GetScriptTemplates => this.standardTemplates;

        public override void RegisterStandardScriptTemplate(ScriptTemplate scriptTemplate)
        {
            if (!this.standardTemplates.Any(template => (template.Type == scriptTemplate.Type)))
            {
                this.standardTemplates.Add(scriptTemplate);
            }
        }


        public override ScriptTemplate GetTemplateFromScriptPubKey(Script script)
        {
            return this.standardTemplates.FirstOrDefault(t => t.CheckScriptPubKey(script));
        }

    }
}
