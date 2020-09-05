using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class TransactionDataTest
    {
        [Fact]
        public void IsConfirmedWithTransactionHavingBlockHeightReturnsTrue()
        {
            var transaction = new TransactionOutputData
            {
                BlockHeight = 15
            };

            Assert.True(transaction.IsConfirmed());
        }

        [Fact]
        public void IsConfirmedWithTransactionHavingNoBlockHeightReturnsFalse()
        {
            var transaction = new TransactionOutputData
            {
                BlockHeight = null
            };

            Assert.False(transaction.IsConfirmed());
        }

        [Fact]
        public void IsSpentWithTransactionHavingSpendingDetailsReturnsTrue()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails()
            };

            Assert.True(transaction.IsSpent());
        }

        [Fact]
        public void IsSpentWithTransactionHavingNoSpendingDetailsReturnsFalse()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = null
            };

            Assert.False(transaction.IsSpent());
        }

        [Fact]
        public void UnspentAmountNotConfirmedOnlyGivenNoSpendingDetailsReturnsTransactionAmount()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = null,
                Amount = new Money(15)
            };

            Money result = transaction.GetUnspentAmount(false);

            Assert.Equal(new Money(15), result);
        }

        [Fact]
        public void UnspentAmountNotConfirmedOnlyGivenBeingConfirmedAndSpentConfirmedReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails { BlockHeight = 16 },
                Amount = new Money(15),
                BlockHeight = 15
            };

            Money result = transaction.GetUnspentAmount(false);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountNotConfirmedOnlyGivenBeingConfirmedAndSpentUnconfirmedReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails(),
                Amount = new Money(15),
                BlockHeight = 15
            };

            Money result = transaction.GetUnspentAmount(false);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountConfirmedOnlyGivenBeingConfirmedAndSpentUnconfirmedReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails(),
                Amount = new Money(15),
                BlockHeight = 15
            };

            Money result = transaction.GetUnspentAmount(true);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountNotConfirmedOnlyGivenBeingUnConfirmedAndSpentUnconfirmedReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails(),
                Amount = new Money(15),
            };

            Money result = transaction.GetUnspentAmount(false);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountConfirmedOnlyGivenNoSpendingDetailsReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = null
            };

            Money result = transaction.GetUnspentAmount(true);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountConfirmedOnlyGivenBeingConfirmedAndSpentConfirmedReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails { BlockHeight = 16 },
                Amount = new Money(15),
                BlockHeight = 15
            };

            Money result = transaction.GetUnspentAmount(true);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountConfirmedOnlyGivenBeingUnConfirmedAndSpentUnconfirmedReturnsZero()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = new SpendingDetails(),
                Amount = new Money(15),
            };

            Money result = transaction.GetUnspentAmount(true);

            Assert.Equal(Money.Zero, result);
        }

        [Fact]
        public void UnspentAmountConfirmedOnlyGivenSpendableAndConfirmedReturnsAmount()
        {
            var transaction = new TransactionOutputData
            {
                SpendingDetails = null,
                Amount = new Money(15),
                BlockHeight = 15
            };

            Money result = transaction.GetUnspentAmount(true);

            Assert.Equal(new Money(15), result);
        }
    }
}
