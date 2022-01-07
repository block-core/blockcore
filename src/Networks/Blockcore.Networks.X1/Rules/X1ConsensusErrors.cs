using Blockcore.Consensus;

namespace Blockcore.Networks.X1.Rules
{
    public static class X1ConsensusErrors
    {
        public static ConsensusError OutputNotWhitelisted => new ConsensusError("tx-output-not-whitelisted", "Only P2WPKH, P2WSH and OP_RETURN are allowed outputs.");

        public static ConsensusError MissingWitness => new ConsensusError("tx-input-missing-witness", "All transaction inputs must have a non-empty WitScript.");

        public static ConsensusError ScriptSigNotEmpty => new ConsensusError("scriptsig-not-empty", "The ScriptSig must be empty.");

        public static ConsensusError FeeBelowAbsoluteMinTxFee => new ConsensusError("fee_below_abolute_min_tx_fee", "The fee must not be below the absolute minimum transaction fee.");

        public static ConsensusError BadPosPowRatchetSequence => new ConsensusError("bad-pos-pow-ratchet-sequence", "Blocks at even heights must be PoS and at odd heights must be PoW.");
    }
}