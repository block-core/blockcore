using System;
using Blockcore.Configuration;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Networks.Xds.Configuration
{
    /// <summary>
    /// Checks that are done before touching the memory pool.
    /// These checks don't need to run under the memory pool lock,
    /// but since we run them now in the rules engine, they do.
    /// </summary>
    public class XdsPreMempoolChecksMempoolRule : MempoolRule
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly NodeSettings nodeSettings;
        private readonly CheckPowTransactionRule checkPowTransactionRule;

        public XdsPreMempoolChecksMempoolRule(Network network,
            ITxMempool txMempool,
            MempoolSettings mempoolSettings,
            ChainIndexer chainIndexer,
            IConsensusRuleEngine consensusRules,
            IDateTimeProvider dateTimeProvider,
            NodeSettings nodeSettings,
            ILoggerFactory loggerFactory) : base(network, txMempool, mempoolSettings, chainIndexer, loggerFactory)
        {
            this.dateTimeProvider = dateTimeProvider;
            this.nodeSettings = nodeSettings;

            this.checkPowTransactionRule = consensusRules.GetRule<CheckPowTransactionRule>();
        }

        public override void CheckTransaction(MempoolValidationContext context)
        {
            Transaction transaction = context.Transaction;

            // Coinbase is only valid in a block, not as a loose transaction
            if (context.Transaction.IsCoinBase)
            {
                this.logger.LogTrace("(-)[FAIL_INVALID_COINBASE]");
                context.State.Fail(MempoolErrors.Coinbase).Throw();
            }

            // Coinstake is only valid in a block, not as a loose transaction
            if (transaction.IsCoinStake)
            {
                this.logger.LogTrace("(-)[FAIL_INVALID_COINSTAKE]");
                context.State.Fail(MempoolErrors.Coinstake).Throw();
            }

            this.checkPowTransactionRule.CheckTransaction(this.network, this.network.Consensus.Options, transaction);

            // we do not need the CheckPosTransactionRule, the checks for empty outputs are already in the XdsOutputNotWhitelistedMempoolRule.

            CheckStandardTransaction(context);

            // ObsidianX behaves according do BIP 113. Since the adoption of BIP 113, the time-based nLockTime is compared to the 11 - block median time past
            // (the median timestamp of the 11 blocks preceding the block in which the transaction is mined), and not the block time itself.
            // The median time past tends to lag the current unix time by about one hour(give or take), but unlike block time it increases monotonically.
            // For transaction relay, nLockTime must be <= the current block's height (block-based) or <= the current median time past (if time based).
            // This ensures that the transaction can be included in the next block.
            if (!CheckFinalTransaction(this.chainIndexer, this.dateTimeProvider, context.Transaction))
            {
                this.logger.LogTrace("(-)[FAIL_NONSTANDARD]");
                context.State.Fail(MempoolErrors.NonFinal).Throw();
            }
        }

        /// <summary>
        /// Validate the transaction is a standard transaction. Checks the version number, transaction size, input signature size,
        /// output script template, single output, & dust outputs.
        /// <seealso>
        ///     <cref>https://github.com/bitcoin/bitcoin/blob/aa624b61c928295c27ffbb4d27be582f5aa31b56/src/policy/policy.cpp##L82-L144</cref>
        /// </seealso>
        /// </summary>
        /// <remarks>Note that, unfortunately, this does not constitute everything that can be non-standard about a transaction.
        /// For example, there may be script verification flags that are not consensus-mandatory but are part of the standardness checks.
        /// These verify flags are checked elsewhere.</remarks>
        /// <param name="context">Current validation context.</param>
        private void CheckStandardTransaction(MempoolValidationContext context)
        {
            Transaction tx = context.Transaction;
            if (tx.Version > this.network.Consensus.Options.MaxStandardVersion || tx.Version < 1)
            {
                this.logger.LogTrace("(-)[FAIL_TX_VERSION]");
                context.State.Fail(MempoolErrors.Version).Throw();
            }

            int dataOut = 0;
            foreach (TxOut txOut in tx.Outputs)
            {
                // this rule runs very early in the validation pipeline, check basics as well.
                if (txOut?.ScriptPubKey == null || txOut.ScriptPubKey.Length < 1)
                {
                    this.logger.LogTrace("(-)[FAIL_EMPTY_SCRIPTPUBKEY]");
                    context.State.Fail(MempoolErrors.Scriptpubkey).Throw();
                }

                // Output checking is already implemented in the XdsOutputNotWhitelistedRule
                // and XdsOutputNotWhitelistedMempoolRule.

                // OP_RETURN
                byte[] raw = txOut.ScriptPubKey.ToBytes();
                if (raw[0] == (byte)OpcodeType.OP_RETURN)
                    dataOut++;

                if (txOut.IsDust(this.nodeSettings.MinRelayTxFeeRate))
                {
                    this.logger.LogTrace("(-)[FAIL_DUST]");
                    context.State.Fail(MempoolErrors.Dust).Throw();
                }
            }

            // Only one OP_RETURN txOut is permitted
            if (dataOut > 1)
            {
                this.logger.LogTrace("(-)[FAIL_MULTI_OPRETURN]");
                context.State.Fail(MempoolErrors.MultiOpReturn).Throw();
            }
        }

        /// <summary>
        /// Validates that the transaction is the final transaction with the time rules
        /// according to BIP-113 rules.
        /// </summary>
        /// <param name="chainIndexer">Block chain used for computing time-locking on the transaction.</param>
        /// <param name="dateTimeProvider">Provides the current date and time.</param>
        /// <param name="tx">The transaction to validate.</param>
        /// <returns>Whether the final transaction was valid.</returns>
        /// <seealso cref="Transaction.IsFinal(DateTimeOffset, int)"/>
        private static bool CheckFinalTransaction(ChainIndexer chainIndexer, IDateTimeProvider dateTimeProvider, Transaction tx)
        {
            // CheckFinalTx() uses chainActive.Height()+1 to evaluate
            // nLockTime because when IsFinalTx() is called within
            // CBlock::AcceptBlock(), the height of the block *being*
            // evaluated is what is used. Thus if we want to know if a
            // transaction can be part of the *next* block, we need to call
            // IsFinalTx() with one more than chainActive.Height().
            int blockHeight = chainIndexer.Height + 1;

            // BIP113 requires that time-locked transactions have nLockTime set to
            // less than the median time of the previous block they're contained in.
            // When the next block is created its previous block will be the current
            // chain tip, so we use that to calculate the median time passed to
            // IsFinalTx().
            DateTimeOffset blockTime = DateTimeOffset.FromUnixTimeMilliseconds(dateTimeProvider.GetTime());

            return tx.IsFinal(blockTime, blockHeight);
        }
    }
}