using System.Collections.Generic;
using Blockcore.Networks;
using NBitcoin.BitcoinCore;

namespace Blockcore.Consensus.Script
{
    public interface IStandardScriptsRegistry
    {
        void RegisterStandardScriptTemplate(ScriptTemplate scriptTemplate);

        bool IsStandardTransaction(Transaction.Transaction tx, Network network);

        bool AreOutputsStandard(Network network, Transaction.Transaction tx);

        ScriptTemplate GetTemplateFromScriptPubKey(Script script);

        bool IsStandardScriptPubKey(Network network, Script scriptPubKey);

        bool AreInputsStandard(Network network, Transaction.Transaction tx, CoinsView coinsView);

        List<ScriptTemplate> GetScriptTemplates { get; }
    }
}