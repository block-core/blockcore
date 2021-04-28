using Blockcore.Consensus;
using NBitcoin;

namespace Blockcore.Networks.Xds.Consensus
{
    /// <inheritdoc />
    public class XdsConsensusOptions : PosConsensusOptions
    {
        /// <inheritdoc />
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            // StakeMinConfirmations must equal MaxReorgLength so that nobody can stake in isolation and then force a reorg
            return (int)network.Consensus.MaxReorgLength;
        }
    }
}