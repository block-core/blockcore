using System;
using System.Threading.Tasks;
using Blockcore.Connection.Broadcasting;
using NBitcoin;

namespace Blockcore.Interfaces
{
    public interface IBroadcasterManager
    {
        Task BroadcastTransactionAsync(Transaction transaction);

        event EventHandler<TransactionBroadcastEntry> TransactionStateChanged;

        TransactionBroadcastEntry GetTransaction(uint256 transactionHash);

        void AddOrUpdate(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string mempoolError = null);
    }
}