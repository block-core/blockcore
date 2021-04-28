using Blockcore.Consensus;
using Blockcore.Networks;
using NBitcoin;

namespace Rutanio.Networks.Consensus
{
    /// <inheritdoc />
    public class RutanioPosConsensusOptions : PosConsensusOptions
    {
        /// <inheritdoc />
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            // StakeMinConfirmations must equal MaxReorgLength so that nobody can stake in isolation and then force a reorg
            return (int)network.Consensus.MaxReorgLength;
        }
    }
}