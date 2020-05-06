using System;
using System.Threading.Tasks;
using Blockcore.Connection.Broadcasting;
using NBitcoin;

namespace Blockcore.Interfaces
{
    /// <summary>
    /// Allow to check a transaction is valid before broadcasting it.
    /// </summary>
    public interface IBroadcastCheck
    {
        Task<string> CheckTransaction(Transaction transaction);
    }

    public interface IBroadcasterManager
    {
        Task BroadcastTransactionAsync(Transaction transaction);

        BroadcastTransactionStateChanedEntry GetTransaction(uint256 transactionHash);

        void AddOrUpdate(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage = null);
    }
}