using Blockcore.Consensus;

namespace Blockcore.Networks.x42.Networks.Consensus
{
    public class x42PosConsensusOptions : PosConsensusOptions
    {
        /// <inheritdoc />
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            // StakeMinConfirmations must equal MaxReorgLength so that nobody can stake in isolation and then force a reorg
            return (int)network.Consensus.MaxReorgLength;
        }
    }
}