﻿using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.NBitcoin.BitcoinCore;

namespace Blockcore.Networks.Stratis.Policies
{
    /// <summary>
    /// Stratis-specific standard transaction definitions.
    /// </summary>
    public class StratisStandardScriptsRegistry : StandardScriptsRegistry
    {
        // See MAX_OP_RETURN_RELAY in stratisX, <script.h>
        public const int MaxOpReturnRelay = 40;

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

        public override bool IsStandardTransaction(Transaction tx, Network network)
        {
            return base.IsStandardTransaction(tx, network);
        }

        public override bool AreOutputsStandard(Network network, Transaction tx)
        {
            return base.AreOutputsStandard(network, tx);
        }

        public override ScriptTemplate GetTemplateFromScriptPubKey(Script script)
        {
            return this.standardTemplates.FirstOrDefault(t => t.CheckScriptPubKey(script));
        }

        public override bool IsStandardScriptPubKey(Network network, Script scriptPubKey)
        {
            return base.IsStandardScriptPubKey(network, scriptPubKey);
        }

        public override bool AreInputsStandard(Network network, Transaction tx, CoinsView coinsView)
        {
            return base.AreInputsStandard(network, tx, coinsView);
        }
    }
}