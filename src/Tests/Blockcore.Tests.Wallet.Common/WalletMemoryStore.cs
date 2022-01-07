using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Microsoft.Data.Sqlite;
using Moq;
using NBitcoin;

namespace Blockcore.Features.Wallet.Tests
{
    public class WalletMemoryStore : WalletStore
    {
        public SqliteConnection MasterSqliteConnection { get; set; }

        public WalletMemoryStore() : this(KnownNetworks.StratisTest)
        {
        }

        public WalletMemoryStore(Network network) : base(network ?? KnownNetworks.StratisTest, new Types.Wallet { Name = "Wallet", EncryptedSeed = "EncryptedSeed" })
        {
        }

        public void Add(IEnumerable<TransactionOutputData> transactions)
        {
            transactions.ToList().ForEach(this.InsertOrUpdate);
        }
    }
}