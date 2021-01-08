using Blockcore.Features.Consensus.Rules.UtxosetRules;
using NBitcoin;

namespace Blockcore.Networks.City.Networks.Rules
{
    /// <summary>
    /// Proof of stake override for the coinview rules - BIP68, MaxSigOps and BlockReward checks.
    /// The City rule reduces the coinbase reward from 20 to 2 at block height 1 111 111?
    /// </summary>
    public class CityCheckPosUtxosetRule : CheckPosUtxosetRule
    {
        private static readonly int REDUCTIONHEIGHT = 1111111;

        /// <inheritdoc />
        public override Money GetProofOfWorkReward(int height)
        {
            if (this.IsPremine(height))
                return this.consensus.PremineReward;

            return this.consensus.ProofOfWorkReward;
        }

        /// <summary>
        /// Gets miner's coin stake reward.
        /// </summary>
        /// <param name="height">Target block height.</param>
        /// <returns>Miner's coin stake reward.</returns>
        public override Money GetProofOfStakeReward(int height)
        {
            if (this.IsPremine(height))
                return this.consensus.PremineReward;

            if (height > REDUCTIONHEIGHT)
            {
                return this.consensus.ProofOfStakeReward / 10;
            }

            return this.consensus.ProofOfStakeReward;
        }
    }
}
