using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.Tests.Common;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class WalletTest : WalletTestBase
    {
        [Fact]
        public void GetAccountsWithoutAccountsReturnsEmptyList()
        {
            var wallet = new Types.Wallet();

            IEnumerable<HdAccount> result = wallet.GetAccounts();

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllTransactionsReturnsTransactionsFromWallet()
        {
            var wallet = new Types.Wallet();
            wallet.walletStore = new WalletMemoryStore();
            AccountRoot stratisAccountRoot = CreateAccountRootWithHdAccountHavingAddresses("StratisAccount", KnownCoinTypes.Stratis);

            TransactionOutputData transaction1 = CreateTransaction(new uint256(1), new Money(15000), 1);
            TransactionOutputData transaction2 = CreateTransaction(new uint256(2), new Money(91209), 1);

            transaction1.OutPoint = new OutPoint(new uint256(1), 1);
            transaction1.Address = stratisAccountRoot.Accounts.ElementAt(0).InternalAddresses.ElementAt(0).Address;
            wallet.walletStore.InsertOrUpdate(transaction1);
            transaction2.OutPoint = new OutPoint(new uint256(2), 1);
            transaction2.Address = stratisAccountRoot.Accounts.ElementAt(0).ExternalAddresses.ElementAt(0).Address;
            wallet.walletStore.InsertOrUpdate(transaction2);

            wallet.AccountsRoot.Add(stratisAccountRoot);

            List<TransactionOutputData> result = wallet.GetAllTransactions().ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(transaction1.OutPoint, result.Select(x => x.OutPoint));
            Assert.Contains(transaction2.OutPoint, result.Select(x => x.OutPoint));
        }

        [Fact]
        public void GetAllTransactionsWithoutAccountRootReturnsEmptyList()
        {
            var wallet = new Types.Wallet();
            wallet.walletStore = new WalletMemoryStore();

            List<TransactionOutputData> result = wallet.GetAllTransactions().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void GetAllPubKeysReturnsPubkeysFromWallet()
        {
            var wallet = new Types.Wallet();
            AccountRoot stratisAccountRoot = CreateAccountRootWithHdAccountHavingAddresses("StratisAccount", KnownCoinTypes.Stratis);
            wallet.AccountsRoot.Add(stratisAccountRoot);

            List<Script> result = wallet.GetAllPubKeys().ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(stratisAccountRoot.Accounts.ElementAt(0).ExternalAddresses.ElementAt(0).ScriptPubKey, result[0]);
            Assert.Equal(stratisAccountRoot.Accounts.ElementAt(0).InternalAddresses.ElementAt(0).ScriptPubKey, result[1]);
        }

        [Fact]
        public void GetAllPubKeysWithoutAccountRootsReturnsEmptyList()
        {
            var wallet = new Types.Wallet();

            List<Script> result = wallet.GetAllPubKeys().ToList();

            Assert.Empty(result);
        }
    }
}