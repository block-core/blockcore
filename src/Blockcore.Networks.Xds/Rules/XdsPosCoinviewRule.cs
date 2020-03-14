using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.Xds.Rules
{
    public sealed class XdsPosCoinviewRule : CheckPosUtxosetRule
    {
        /// <inheritdoc />
        public override Money GetProofOfWorkReward(int height)
        {
            int halvings = height / this.consensus.SubsidyHalvingInterval;

            if (halvings >= 64)
                return 0;

            Money subsidy = this.consensus.ProofOfWorkReward;

            subsidy >>= halvings;

            return subsidy;
        }

        /// <inheritdoc />
        public override Money GetProofOfStakeReward(int height)
        {
            int halvings = height / this.consensus.SubsidyHalvingInterval;

            if (halvings >= 64)
                return 0;

            Money subsidy = this.consensus.ProofOfStakeReward;

            subsidy >>= halvings;

            return subsidy;
        }

        protected override Money GetTransactionFee(UnspentOutputSet view, Transaction tx)
        {
            Money fee = base.GetTransactionFee(view, tx);

            if (!tx.IsProtocolTransaction())
            {
                if (fee < ((XdsMain)this.Parent.Network).AbsoluteMinTxFee)
                {
                    this.Logger.LogTrace($"(-)[FAIL_{nameof(XdsRequireWitnessRule)}]".ToUpperInvariant());
                    XdsConsensusErrors.FeeBelowAbsoluteMinTxFee.Throw();
                }
            }

            return fee;
        }

        protected override void CheckInputValidity(Transaction transaction, UnspentOutput coins)
        {
            return;
        }
    }
}