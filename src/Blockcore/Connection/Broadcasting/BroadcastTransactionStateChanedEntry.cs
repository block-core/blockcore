using System;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcastTransactionStateChanedEntry
    {
        public NBitcoin.Transaction Transaction { get; }

        public TransactionBroadcastState TransactionBroadcastState { get; set; }

        public string ErrorMessage => (this.MempoolError == null) ? string.Empty : $"Failed: {this.ErrorMessage}";

        public string MempoolError { get; set; }

        public BroadcastTransactionStateChanedEntry(NBitcoin.Transaction transaction, TransactionBroadcastState transactionBroadcastState, string mempoolError)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.TransactionBroadcastState = transactionBroadcastState;
            this.MempoolError = mempoolError;
        }
    }
}