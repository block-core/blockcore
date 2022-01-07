using Blockcore.Networks.Strax.Federation;

namespace Blockcore.Networks.Strax
{
    /// <summary>
    /// Contains properties used by the Strax network definitions that are not present in the base network class.
    /// </summary>
    public class StraxBaseNetwork : Network
    {
        public IFederations Federations { get; protected set; }

        /// <summary> This is used for reward distribution transactions. </summary>
        public string CirrusRewardDummyAddress { get; protected set; }
    }
}