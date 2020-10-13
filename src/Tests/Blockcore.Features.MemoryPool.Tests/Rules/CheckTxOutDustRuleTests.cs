using Blockcore.Configuration;
using Blockcore.Configuration.Logging;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.MemoryPool.Rules;
using Blockcore.Networks;
using Blockcore.Networks.Stratis;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.MemoryPool.Tests.Rules
{
    public sealed class CheckTxOutDustRuleTests
    {
        private readonly ChainIndexer chainIndexer;
        private readonly ILoggerFactory loggerFactory;
        private readonly Network network;
        private readonly NodeSettings nodeSettings;
        private readonly ITxMempool txMempool;

        public CheckTxOutDustRuleTests()
        {
            this.network = new StratisMain();
            this.chainIndexer = new ChainIndexer(this.network);
            this.loggerFactory = ExtendedLoggerFactory.Create();
            this.nodeSettings = NodeSettings.Default(this.network);
            this.txMempool = new Mock<ITxMempool>().Object;
        }

        [Fact]
        public void CheckTxOutDustRule_Pass()
        {
            var rule = new CheckTxOutDustRule(this.network, this.txMempool, new MempoolSettings(this.nodeSettings), this.chainIndexer, this.loggerFactory);
            var transaction = CreateTransaction(Money.Coins(1));
            var mempoolValidationContext = new MempoolValidationContext(transaction, new MempoolValidationState(false))
            {
                MinRelayTxFee = this.nodeSettings.MinRelayTxFeeRate,
                ValueOut = transaction.TotalOut
            };

            rule.CheckTransaction(mempoolValidationContext);
            Assert.Null(mempoolValidationContext.State.Error);
        }

        [Fact]
        public void CheckTxOutDustRule_TxOut_Is_OpReturn_Pass()
        {
            var rule = new CheckTxOutDustRule(this.network, this.txMempool, new MempoolSettings(this.nodeSettings), this.chainIndexer, this.loggerFactory);
            var transaction = CreateTransaction(Money.Coins(1), true);
            var mempoolValidationContext = new MempoolValidationContext(transaction, new MempoolValidationState(false))
            {
                MinRelayTxFee = this.nodeSettings.MinRelayTxFeeRate,
                ValueOut = transaction.TotalOut
            };

            rule.CheckTransaction(mempoolValidationContext);
            Assert.Null(mempoolValidationContext.State.Error);
        }

        [Fact]
        public void CheckTxOutDustRule_TxOut_Is_OpReturn_WithDustTxOut_Fail()
        {
            var rule = new CheckTxOutDustRule(this.network, this.txMempool, new MempoolSettings(this.nodeSettings), this.chainIndexer, this.loggerFactory);

            // Create tx with OpReturn output.
            var transaction = CreateTransaction(Money.Coins(1), true);

            // Add a dust output.
            transaction.AddOutput(new TxOut(Money.Coins(0.000001m), new Script()));

            var mempoolValidationContext = new MempoolValidationContext(transaction, new MempoolValidationState(false))
            {
                MinRelayTxFee = this.nodeSettings.MinRelayTxFeeRate,
                ValueOut = transaction.TotalOut
            };

            Assert.Throws<MempoolErrorException>(() => rule.CheckTransaction(mempoolValidationContext));
            Assert.NotNull(mempoolValidationContext.State.Error);
            Assert.Equal(MempoolErrors.TransactionContainsDustTxOuts, mempoolValidationContext.State.Error);
        }

        [Fact]
        public void CheckTxOutDustRule_Fail()
        {
            var rule = new CheckTxOutDustRule(this.network, this.txMempool, new MempoolSettings(this.nodeSettings), this.chainIndexer, this.loggerFactory);
            var transaction = CreateTransaction(Money.Coins(0.000001m));
            var mempoolValidationContext = new MempoolValidationContext(transaction, new MempoolValidationState(false))
            {
                MinRelayTxFee = this.nodeSettings.MinRelayTxFeeRate,
                ValueOut = transaction.TotalOut
            };

            Assert.Throws<MempoolErrorException>(() => rule.CheckTransaction(mempoolValidationContext));
            Assert.NotNull(mempoolValidationContext.State.Error);
            Assert.Equal(MempoolErrors.TransactionContainsDustTxOuts, mempoolValidationContext.State.Error);
        }

        private Transaction CreateTransaction(Money amount, bool isOpReturn = false)
        {
            var transaction = this.network.CreateTransaction();

            transaction.AddInput(TxIn.CreateCoinbase(1));

            if (isOpReturn)
                transaction.AddOutput(new TxOut(0, new Script(OpcodeType.OP_RETURN, Op.GetPushOp(new Key().PubKey.Compress().ToBytes()))));
            else
                transaction.AddOutput(new TxOut(amount, new Script()));

            return transaction;
        }
    }
}
