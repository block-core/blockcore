using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.Tests.Common;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class HdAccountTest
    {
        [Fact]
        public void GetCoinTypeHavingHdPathReturnsCointType()
        {
            var account = new HdAccount();
            account.HdPath = "1/2/105";

            int result = account.GetCoinType();

            Assert.Equal(KnownCoinTypes.Stratis, result);
        }

        [Fact]
        public void GetCoinTypeWithInvalidHdPathThrowsFormatException()
        {
            Assert.Throws<FormatException>(() =>
            {
                var account = new HdAccount();
                account.HdPath = "1/";

                account.GetCoinType();
            });
        }

        [Fact]
        public void GetCoinTypeWithoutHdPathThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
           {
               var account = new HdAccount();
               account.HdPath = null;

               account.GetCoinType();
           });
        }

        [Fact]
        public void GetCoinTypeWithEmptyHdPathThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
           {
               var account = new HdAccount();
               account.HdPath = string.Empty;

               account.GetCoinType();
           });
        }

        [Fact]
        public void GetFirstUnusedReceivingAddressWithExistingUnusedReceivingAddressReturnsAddressWithLowestIndex()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Add(new HdAddress { Index = 3, Address = "3" });
            account.ExternalAddresses.Add(new HdAddress { Index = 2, Address = "2" });
            account.ExternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "1" } });

            HdAddress result = account.GetFirstUnusedReceivingAddress(store);

            Assert.Equal(account.ExternalAddresses.ElementAt(1), result);
        }

        [Fact]
        public void GetFirstUnusedReceivingAddressWithoutExistingUnusedReceivingAddressReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Add(new HdAddress { Index = 2, Address = "2" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "2" } });
            account.ExternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(2), 1), Address = "1" } });

            HdAddress result = account.GetFirstUnusedReceivingAddress(store);

            Assert.Null(result);
        }

        [Fact]
        public void GetFirstUnusedReceivingAddressWithoutReceivingAddressReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Clear();

            HdAddress result = account.GetFirstUnusedReceivingAddress(store);

            Assert.Null(result);
        }

        [Fact]
        public void GetFirstUnusedChangeAddressWithExistingUnusedChangeAddressReturnsAddressWithLowestIndex()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.InternalAddresses.Add(new HdAddress { Index = 3, Address = "3" });
            account.InternalAddresses.Add(new HdAddress { Index = 2, Address = "2" });
            account.InternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "1" } });

            HdAddress result = account.GetFirstUnusedChangeAddress(store);

            Assert.Equal(account.InternalAddresses.ElementAt(1), result);
        }

        [Fact]
        public void GetFirstUnusedChangeAddressWithoutExistingUnusedChangeAddressReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.InternalAddresses.Add(new HdAddress { Index = 2, Address = "2" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "2" } });
            account.InternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(2), 1), Address = "1" } });

            HdAddress result = account.GetFirstUnusedChangeAddress(store);

            Assert.Null(result);
        }

        [Fact]
        public void GetFirstUnusedChangeAddressWithoutChangeAddressReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.InternalAddresses.Clear();

            HdAddress result = account.GetFirstUnusedChangeAddress(store);

            Assert.Null(result);
        }

        [Fact]
        public void GetLastUsedAddressWithChangeAddressesHavingTransactionsReturnsHighestIndex()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.InternalAddresses.Add(new HdAddress { Index = 2, Address = "2" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "2" } });
            account.InternalAddresses.Add(new HdAddress { Index = 3, Address = "3" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(2), 1), Address = "3" } });
            account.InternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(3), 1), Address = "1" } });

            HdAddress result = account.GetLastUsedAddress(store, isChange: true);

            Assert.Equal(account.InternalAddresses.ElementAt(1), result);
        }

        [Fact]
        public void GetLastUsedAddressLookingForChangeAddressWithoutChangeAddressesHavingTransactionsReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.InternalAddresses.Add(new HdAddress { Index = 2, Address = "2" });
            account.InternalAddresses.Add(new HdAddress { Index = 3, Address = "3" });
            account.InternalAddresses.Add(new HdAddress { Index = 1, Address = "1" });

            HdAddress result = account.GetLastUsedAddress(store, isChange: true);

            Assert.Null(result);
        }

        [Fact]
        public void GetLastUsedAddressLookingForChangeAddressWithoutChangeAddressesReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.InternalAddresses.Clear();

            HdAddress result = account.GetLastUsedAddress(store, isChange: true);

            Assert.Null(result);
        }

        [Fact]
        public void GetLastUsedAddressWithReceivingAddressesHavingTransactionsReturnsHighestIndex()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Add(new HdAddress { Index = 2, Address = "2" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "2" } });
            account.ExternalAddresses.Add(new HdAddress { Index = 3, Address = "3" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(2), 1), Address = "3" } });
            account.ExternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(3), 1), Address = "1" } });

            HdAddress result = account.GetLastUsedAddress(store, isChange: false);

            Assert.Equal(account.ExternalAddresses.ElementAt(1), result);
        }

        [Fact]
        public void GetLastUsedAddressLookingForReceivingAddressWithoutReceivingAddressesHavingTransactionsReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Add(new HdAddress { Index = 2, Address = "2" });
            account.ExternalAddresses.Add(new HdAddress { Index = 3, Address = "3" });
            account.ExternalAddresses.Add(new HdAddress { Index = 1, Address = "1" });

            HdAddress result = account.GetLastUsedAddress(store, isChange: false);

            Assert.Null(result);
        }

        [Fact]
        public void GetLastUsedAddressLookingForReceivingAddressWithoutReceivingAddressesReturnsNull()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Clear();

            HdAddress result = account.GetLastUsedAddress(store, isChange: false);

            Assert.Null(result);
        }

        //[Fact]
        //public void GetTransactionsByIdHavingTransactionsWithIdReturnsTransactions()
        //{
        //    var store = new WalletMemoryStore();
        //    var account = new HdAccount();
        //    account.ExternalAddresses.Add(new HdAddress {Index = 2, Address = "2", Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(15), Index = 7 } } });
        //    account.ExternalAddresses.Add(new HdAddress {Index = 3, Address = "3", Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(18), Index = 8 } } });
        //    account.ExternalAddresses.Add(new HdAddress {Index = 1, Address = "1", Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(19), Index = 9 } } });
        //    account.ExternalAddresses.Add(new HdAddress { Index = 6, Transactions = null });

        //    account.InternalAddresses.Add(new HdAddress { Index = 4, Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(15),Index = 1, Address = "1"0 } } });
        //    account.InternalAddresses.Add(new HdAddress { Index = 5, Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(18),Index = 1, Address = "1"1 } } });
        //    account.InternalAddresses.Add(new HdAddress { Index = 6, Transactions = null });
        //    account.InternalAddresses.Add(new HdAddress { Index = 6, Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(19),Index = 1, Address = "1"2 } } });

        //    IEnumerable<TransactionData> result = account.GetTransactionsById( new uint256(18));

        //    Assert.Equal(2, result.Count());
        //    Assert.Equal(8, result.ElementAt(0).Index);
        //    Assert.Equal(new uint256(18), result.ElementAt(0).Id);
        //    Assert.Equal(11, result.ElementAt(1).Index);
        //    Assert.Equal(new uint256(18), result.ElementAt(1).Id);
        //}

        //[Fact]
        //public void GetTransactionsByIdHavingNoMatchingTransactionsReturnsEmptyList()
        //{
        //    var store = new WalletMemoryStore();
        //    var account = new HdAccount();
        //    account.ExternalAddresses.Add(new HdAddress {Index = 2, Address = "2", Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(15), Index = 7 } } });
        //    account.InternalAddresses.Add(new HdAddress { Index = 4, Transactions = new List<TransactionData> { new TransactionData { Id = new uint256(15),Index = 1, Address = "1"0 } } });

        //    IEnumerable<TransactionData> result = account.GetTransactionsById(new uint256(20));

        //    Assert.Empty(result);
        //}

        [Fact]
        public void GetSpendableTransactionsWithSpendableTransactionsReturnsSpendableTransactions()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Add(new HdAddress { Index = 2, Address = "2" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "2", Id = new uint256(15), Index = 7, SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) } } });
            account.ExternalAddresses.Add(new HdAddress { Index = 3, Address = "3" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(2), 1), Address = "3", Id = new uint256(18), Index = 8 } });
            account.ExternalAddresses.Add(new HdAddress { Index = 1, Address = "1" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(3), 1), Address = "1", Id = new uint256(19), Index = 9, SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) } } });
            account.ExternalAddresses.Add(new HdAddress { Index = 6 });

            account.InternalAddresses.Add(new HdAddress { Index = 4, Address = "4" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(4), 1), Address = "4", Id = new uint256(15), Index = 10, SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) } } });
            account.InternalAddresses.Add(new HdAddress { Index = 5, Address = "5" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(5), 1), Address = "5", Id = new uint256(18), Index = 11 } });
            account.InternalAddresses.Add(new HdAddress { Index = 6 });
            account.InternalAddresses.Add(new HdAddress { Index = 6, Address = "6" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(6), 1), Address = "6", Id = new uint256(19), Index = 12, SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) } } });

            IEnumerable<UnspentOutputReference> result = account.GetSpendableTransactions(store, 100, 10, 0);

            Assert.Equal(2, result.Count());
            Assert.Equal(8, result.ElementAt(0).Transaction.Index);
            Assert.Equal(new uint256(18), result.ElementAt(0).Transaction.Id);
            Assert.Equal(11, result.ElementAt(1).Transaction.Index);
            Assert.Equal(new uint256(18), result.ElementAt(1).Transaction.Id);
        }

        [Fact]
        public void GetSpendableTransactionsWithoutSpendableTransactionsReturnsEmptyList()
        {
            var store = new WalletMemoryStore();
            var account = new HdAccount();
            account.ExternalAddresses.Add(new HdAddress { Index = 2, Address = "2" }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(1), 1), Address = "2", Id = new uint256(15), Index = 7, SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) } } });
            account.InternalAddresses.Add(new HdAddress { Index = 4 }); store.Add(new List<TransactionOutputData> { new TransactionOutputData { OutPoint = new OutPoint(new uint256(2), 1), Address = "4", Id = new uint256(15), Index = 10, SpendingDetails =  new SpendingDetails { TransactionId = new uint256(1) } } });

            IEnumerable<UnspentOutputReference> result = account.GetSpendableTransactions(store, 100, 10, 0);

            Assert.Empty(result);
        }

        //[Fact]
        //public void FindAddressesForTransactionWithMatchingTransactionsReturnsTransactions()
        //{
        //    var store = new WalletMemoryStore();
        //    var account = new HdAccount();
        //    account.ExternalAddresses.Add(new HdAddress {Index = 2, Address = "2" }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(15), Index = 7 } });
        //    account.ExternalAddresses.Add(new HdAddress {Index = 3, Address = "3" }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(18), Index = 8 } });
        //    account.ExternalAddresses.Add(new HdAddress {Index = 1, Address = "1" }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(19), Index = 9 } });
        //    account.ExternalAddresses.Add(new HdAddress { Index = 6 });

        //    account.InternalAddresses.Add(new HdAddress { Index = 4 }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(15),Index = 1, Address = "1"0 } });

        //    account.InternalAddresses.Add(new HdAddress { Index = 5 }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(18),Index = 1, Address = "1"1 } });

        //    account.InternalAddresses.Add(new HdAddress { Index = 6 }); store.Add(null);
        //    account.InternalAddresses.Add(new HdAddress { Index = 6 }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(19),Index = 1, Address = "1"2 } });

        //    IEnumerable<HdAddress> result = account.FindAddressesForTransaction(t => t.Id == 18);

        //    Assert.Equal(2, result.Count());
        //    Assert.Equal(3, result.ElementAt(0).Index);
        //    Assert.Equal(5, result.ElementAt(1).Index);
        //}

        //[Fact]
        //public void FindAddressesForTransactionWithoutMatchingTransactionsReturnsEmptyList()
        //{
        //    var store = new WalletMemoryStore();
        //    var account = new HdAccount();
        //    account.ExternalAddresses.Add(new HdAddress {Index = 2, Address = "2" }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(15), Index = 7 } });
        //    account.ExternalAddresses.Add(new HdAddress {Index = 3, Address = "3" }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(18), Index = 8 } });
        //    account.ExternalAddresses.Add(new HdAddress {Index = 1, Address = "1" }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(19), Index = 9 } });
        //    account.ExternalAddresses.Add(new HdAddress { Index = 6 }); store.Add(null);

        //    account.InternalAddresses.Add(new HdAddress { Index = 4 }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(15),Index = 1, Address = "1"0 } });
        //    account.InternalAddresses.Add(new HdAddress { Index = 5 }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(18),Index = 1, Address = "1"1 } });
        //    account.InternalAddresses.Add(new HdAddress { Index = 6 }); store.Add(null);
        //    account.InternalAddresses.Add(new HdAddress { Index = 6 }); store.Add(new List<TransactionData> { new TransactionData { Id = new uint256(19),Index = 1, Address = "1"2 } });

        //    IEnumerable<HdAddress> result = account.FindAddressesForTransaction(t => t.Id == 25);

        //    Assert.Empty(result);
        //}
    }
}