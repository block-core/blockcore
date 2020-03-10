using System;
using System.Threading.Tasks;
using NBitcoin;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Wallet.Broadcasting;

namespace Blockcore.Features.Wallet.Interfaces
{
    public interface IBroadcasterManager
    {
        Task BroadcastTransactionAsync(Transaction transaction);

        event EventHandler<TransactionBroadcastEntry> TransactionStateChanged;

        TransactionBroadcastEntry GetTransaction(uint256 transactionHash);

        void AddOrUpdate(Transaction transaction, TransactionBroadcastState transactionBroadcastState, MempoolError mempoolError = null);
    }
}
