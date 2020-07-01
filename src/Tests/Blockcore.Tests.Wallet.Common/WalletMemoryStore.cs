using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blockcore.Features.Wallet.Types;
using Blockcore.Utilities;
using Moq;
using NBitcoin;

namespace Blockcore.Features.Wallet.Tests
{
    public class WalletMemoryStore : IWalletStore
    {
        public Dictionary<OutPoint, TransactionData> transactions { get; set; } = new Dictionary<OutPoint, TransactionData>();

        public int CountForAddress(string address)
        {
            return this.transactions.Values.Where(t => t.Address == address).Count();
        }

        public IEnumerable<TransactionData> GetForAddress(string address)
        {
            return this.transactions.Values.Where(t => t.Address == address);
        }

        public TransactionData GetForOutput(OutPoint outPoint)
        {
            return this.transactions.TryGet(outPoint);
        }

        public void InsertOrUpdate(TransactionData item)
        {
            this.transactions.Add(item.OutPoint, item);
        }

        public bool Remove(OutPoint outPoint)
        {
            throw new NotImplementedException();
        }

        public void Add(IEnumerable<TransactionData> transactions)
        {
            transactions.ToList().ForEach(this.InsertOrUpdate);
        }
    }
}