using System;
using Blockcore.Consensus.TransactionInfo;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcastTransactionStateChanedEntry
    {
        public Transaction Transaction { get; }

        public TransactionBroadcastState TransactionBroadcastState { get; set; }

        public string ErrorMessage { get; private set; }

        public bool CanRespondToGetData { get; set; }

        public BroadcastTransactionStateChanedEntry(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.TransactionBroadcastState = transactionBroadcastState;
            this.ErrorMessage = (errorMessage == null) ? string.Empty : errorMessage;
        }
    }
}