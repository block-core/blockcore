using NBitcoin;
using Stratis.Bitcoin.Consensus;

namespace Stratis.Bitcoin.Interfaces
{
    /// <summary>
    /// A reader for extracting an address from a Script
    /// </summary>
    public interface IScriptAddressReader
    {
        /// <summary>
        /// Extracts ScriptAddressResult from a given Script, if available. Otherwise return <see cref="string.Empty"/> values.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        ScriptAddressResult GetAddressFromScriptPubKey(Network network, Script script);
    }
}
