using Blockcore.Consensus.ScriptInfo;

namespace Blockcore.NBitcoin
{
    /// <summary>
    /// Represent any type which represent an underlying ScriptPubKey
    /// </summary>
    public interface IDestination
    {
        Script ScriptPubKey
        {
            get;
        }
    }
}
