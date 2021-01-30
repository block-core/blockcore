using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Networks.x42.Networks.Consensus;
using NBitcoin;

namespace Blockcore.Networks.x42.Networks.Rules
{
    public sealed class x42PosCoinviewRule : CheckPosUtxosetRule
    {
        /// <inheritdoc />
        public override Money GetProofOfWorkReward(int height)
        {
            if (IsPremine(height))
                return this.consensus.PremineReward;

            return this.consensus.ProofOfWorkReward;
        }

        /// <inheritdoc />
        public override Money GetProofOfStakeReward(int height)
        {
            Money PoSReward = Money.Zero;
            var networkConsensus = (x42Consensus)this.consensus;

            if (IsPastLastPOWBlock(height))
            {
                if (IsPastSubsidyLimit(height) && IsAtOrBeforeEndOfProofOfStakeReward(height))
                {
                    PoSReward = networkConsensus.ProofOfStakeRewardAfterSubsidyLimit;
                }
                else if (IsAtOrBeforeEndOfProofOfStakeReward(height))
                {
                    PoSReward = this.consensus.ProofOfStakeReward;
                }
                else if (IsPremine(height))
                {
                    PoSReward = this.consensus.PremineReward;
                }
            }
            return PoSReward;
        }

        /// <summary>
        /// Determines whether the block with specified height is past the Subsidy Limit.
        /// </summary>
        /// <param name="height">Block's height.</param>
        /// <returns><c>true</c> if the block with provided height is past the Subsidy Limit, <c>false</c> otherwise.</returns>
        bool IsPastSubsidyLimit(int height)
        {
            var networkConsensus = (x42Consensus)this.consensus;
            return (networkConsensus.SubsidyLimit > 0) &&
                   (networkConsensus.SubsidyLimit > 0) &&
                   (height > networkConsensus.SubsidyLimit);
        }

        /// <summary>
        /// Determines whether the block with specified height is past the Subsidy Limit.
        /// </summary>
        /// <param name="height">Block's height.</param>
        /// <returns><c>true</c> if the block with provided height is past the Subsidy Limit, <c>false</c> otherwise.</returns>
        bool IsPastLastPOWBlock(int height)
        {
            return (this.consensus.LastPOWBlock > 0) &&
                   (this.consensus.LastPOWBlock > 0) &&
                   (height > this.consensus.LastPOWBlock);
        }

        /// <summary>
        /// Determines whether the block with specified height is past the Subsidy Limit.
        /// </summary>
        /// <param name="height">Block's height.</param>
        /// <returns><c>true</c> if the block with provided height is before last proof of stake reward, <c>false</c> otherwise.</returns>
        bool IsAtOrBeforeEndOfProofOfStakeReward(int height)
        {
            var networkConsensus = (x42Consensus)this.consensus;
            return (networkConsensus.LastProofOfStakeRewardHeight > 0) &&
                   (networkConsensus.LastProofOfStakeRewardHeight > 0) &&
                   (height <= networkConsensus.LastProofOfStakeRewardHeight);
        }

    }
}