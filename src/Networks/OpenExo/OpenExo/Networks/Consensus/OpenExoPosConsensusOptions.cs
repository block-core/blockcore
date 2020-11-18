using Blockcore.Consensus;
using Blockcore.Networks;
using NBitcoin;


namespace OpenExo.Networks.Consensus
{
    /// <inheritdoc />
    public class OpenExoPosConsensusOptions : PosConsensusOptions
    {
        /// <summary>Coinstake minimal confirmations softfork activation height for mainnet.</summary>
        public const int OpenExoCoinstakeMinConfirmationActivationHeightMainnet = 500000;

        /// <summary>Coinstake minimal confirmations softfork activation height for testnet.</summary>
        public const int OpenExoCoinstakeMinConfirmationActivationHeightTestnet = 115000;

        public override int GetStakeMinConfirmations(int height, Network network)
        {
            if (network.Name.ToLowerInvariant().Contains("test"))
            {
                return height < OpenExoCoinstakeMinConfirmationActivationHeightTestnet ? 10 : 20;
            }

            // The coinstake confirmation minimum should be 50 until activation at height 500K (~347 days).
            return height < OpenExoCoinstakeMinConfirmationActivationHeightMainnet ? 50 : 500;
        }
    }
}
