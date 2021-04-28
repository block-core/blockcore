using Blockcore.Consensus;

namespace Blockcore.Networks.x42.Networks.Consensus
{
    public static class x42ConsensusErrors
    {
        public static ConsensusError InsufficientOpReturnFee => new ConsensusError("op-return-fee-insufficient", "The OP_RETURN fee is insufficient.");
    }
}