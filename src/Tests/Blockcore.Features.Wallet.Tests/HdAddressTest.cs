using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class HdAddressTest
    {
        [Fact]
        public void IsChangeAddressWithValidHdPathForChangeAddressReturnsTrue()
        {
            var address = new HdAddress
            {
                HdPath = "0/1/2/3/1"
            };

            bool result = address.IsChangeAddress();

            Assert.True(result);
        }

        [Fact]
        public void IsChangeAddressWithValidHdPathForNonChangeAddressReturnsFalse()
        {
            var address = new HdAddress
            {
                HdPath = "0/1/2/3/0"
            };

            bool result = address.IsChangeAddress();

            Assert.False(result);
        }

        [Fact]
        public void IsChangeAddressWithTextInHdPathReturnsFalse()
        {
            var address = new HdAddress
            {
                HdPath = "0/1/2/3/A"
            };

            bool result = address.IsChangeAddress();

            Assert.False(result);
        }

        [Fact]
        public void IsChangeAddressWithInvalidHdPathThrowsFormatException()
        {
            Assert.Throws<FormatException>(() =>
            {
                var address = new HdAddress
                {
                    HdPath = "0/1/2"
                };

                bool result = address.IsChangeAddress();
            });
        }

        [Fact]
        public void IsChangeAddressWithEmptyHdPathThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
           {
               var address = new HdAddress
               {
                   HdPath = string.Empty
               };

               bool result = address.IsChangeAddress();
           });
        }

        [Fact]
        public void IsChangeAddressWithNulledHdPathThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var address = new HdAddress
                {
                    HdPath = null
                };

                bool result = address.IsChangeAddress();
            });
        }

        [Fact]
        public void UnspentTransactionsWithAddressHavingUnspentTransactionsReturnsUnspentTransactions()
        {
            WalletMemoryStore store = new WalletMemoryStore();

            var address = new HdAddress
            {
                Address = "Address"
            };

            store.Add(new List<TransactionOutputData> {
                    new TransactionOutputData { Id = new uint256(15), OutPoint = new OutPoint(new uint256(15),1), Address = "Address"},
                    new TransactionOutputData { Id = new uint256(16), SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) }, OutPoint = new OutPoint(new uint256(16),1), Address = "Address" },
                    new TransactionOutputData { Id = new uint256(17), OutPoint = new OutPoint(new uint256(17),1), Address = "Address"},
                    new TransactionOutputData { Id = new uint256(18), SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) }, OutPoint = new OutPoint(new uint256(18),1), Address = "Address" }
                });

            IEnumerable<TransactionOutputData> result = address.UnspentTransactions(store);

            Assert.Equal(2, result.Count());
            Assert.Contains(new uint256(15), result.Select(x => x.Id));
            Assert.Contains(new uint256(17), result.Select(x => x.Id));
        }

        [Fact]
        public void UnspentTransactionsWithAddressNotHavingUnspentTransactionsReturnsEmptyList()
        {
            WalletMemoryStore store = new WalletMemoryStore();

            var address = new HdAddress
            {
                Address = "Address"
            };

            store.Add(new List<TransactionOutputData> {
                    new TransactionOutputData { Id = new uint256(16), SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) }, OutPoint = new OutPoint(new uint256(16),1), Address = "Address" },
                    new TransactionOutputData { Id = new uint256(18), SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) }, OutPoint = new OutPoint(new uint256(18),1), Address = "Address" }
                });

            IEnumerable<TransactionOutputData> result = address.UnspentTransactions(store);

            Assert.Empty(result);
        }

        [Fact]
        public void UnspentTransactionsWithAddressWithoutTransactionsReturnsEmptyList()
        {
            WalletMemoryStore store = new WalletMemoryStore();

            var address = new HdAddress
            {
                Address = "Address"
            };

            IEnumerable<TransactionOutputData> result = address.UnspentTransactions(store);

            Assert.Empty(result);
        }
    }
}