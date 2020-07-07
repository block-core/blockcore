using System.Collections.Generic;
using Blockcore.Features.Wallet.Types;
using NBitcoin;

namespace Blockcore.Features.Wallet.Database
{
    public interface IWalletStore
    {
        void InsertOrUpdate(TransactionData item);

        IEnumerable<TransactionData> GetForAddress(string address);

        IEnumerable<TransactionData> GetUnspentForAddress(string address);

        int CountForAddress(string address);

        TransactionData GetForOutput(OutPoint outPoint);

        bool Remove(OutPoint outPoint);

        WalletData GetData();

        void SetData(WalletData data);
    }
}