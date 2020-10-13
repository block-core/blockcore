using System.Collections.Generic;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Types;
using NBitcoin;

namespace Blockcore.Features.Wallet.Database
{
    public interface IWalletStore
    {
        void InsertOrUpdate(TransactionOutputData item);

        IEnumerable<TransactionOutputData> GetForAddress(string address);

        IEnumerable<TransactionOutputData> GetUnspentForAddress(string address);

        int CountForAddress(string address);

        TransactionOutputData GetForOutput(OutPoint outPoint);

        bool Remove(OutPoint outPoint);

        WalletData GetData();

        void SetData(WalletData data);

        WalletBalanceResult GetBalanceForAddress(string address, bool excludeColdStake);

        WalletBalanceResult GetBalanceForAccount(int accountIndex, bool excludeColdStake);

        IEnumerable<WalletHistoryData> GetAccountHistory(int accountIndex, bool excludeColdStake, int skip = 0, int take = 100);
    }
}