using System;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcastTransactionStateChanedEntry
    {
        public NBitcoin.Transaction Transaction { get; }

        public TransactionBroadcastState TransactionBroadcastState { get; set; }

        public string ErrorMessage { get; private set; }

        public bool CanRespondToGetData { get; set; }

        public BroadcastTransactionStateChanedEntry(NBitcoin.Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.TransactionBroadcastState = transactionBroadcastState;
            this.ErrorMessage = (errorMessage == null) ? string.Empty : $"Failed: {errorMessage}";
        }
    }
}