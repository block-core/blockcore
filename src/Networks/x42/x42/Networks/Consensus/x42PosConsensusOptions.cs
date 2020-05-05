using NBitcoin;

namespace x42.Networks.Consensus
{
    public class x42PosConsensusOptions : PosConsensusOptions
    {
        /// <summary>
        /// Initializes all values. Used by networks that use block weight rules.
        /// </summary>
        public x42PosConsensusOptions(
            uint maxBlockBaseSize,
            uint maxBlockWeight,
            uint maxBlockSerializedSize,
            int witnessScaleFactor,
            int maxStandardVersion,
            int maxStandardTxWeight,
            int maxBlockSigopsCost,
            int maxStandardTxSigopsCost) : base(maxBlockBaseSize, maxBlockWeight, maxBlockSerializedSize, witnessScaleFactor, maxStandardVersion, maxStandardTxWeight, maxBlockSigopsCost, maxStandardTxSigopsCost)
        {
        }

        /// <summary>
        /// Initializes values for networks that use block size rules.
        /// </summary>
        public x42PosConsensusOptions(
            uint maxBlockBaseSize,
            int maxStandardVersion,
            int maxStandardTxWeight,
            int maxBlockSigopsCost,
            int maxStandardTxSigopsCost,
            int witnessScaleFactor
        ) : base(maxBlockBaseSize, maxStandardVersion, maxStandardTxWeight, maxBlockSigopsCost, maxStandardTxSigopsCost, witnessScaleFactor)
        {
        }

        /// <summary>
        /// Uses base class c'tor with Bitcoin rules.
        /// </summary>
        public x42PosConsensusOptions()
        { }

        /// <inheritdoc />
        public override int GetStakeMinConfirmations(int height, Network network)
        {
            // StakeMinConfirmations must equal MaxReorgLength so that nobody can stake in isolation and then force a reorg
            return (int)network.Consensus.MaxReorgLength;
        }
    }
}