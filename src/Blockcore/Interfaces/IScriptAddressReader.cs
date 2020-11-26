using Blockcore.Consensus;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Interfaces
{
    /// <summary>
    /// A reader for extracting an address from a Script
    /// </summary>
    public interface IScriptAddressReader
    {
        /// <summary>
        /// Extracts an address from a given Script, if available. Otherwise returns <see cref="string.Empty"/>
        /// </summary>
        ScriptAddressResult GetAddressFromScriptPubKey(Network network, Script script);
    }
}