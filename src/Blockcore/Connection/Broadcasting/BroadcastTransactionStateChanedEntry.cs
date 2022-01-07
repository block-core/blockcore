using System;
using Blockcore.Consensus.TransactionInfo;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcastTransactionStateChanedEntry
    {
        [JsonIgnore] // The "Transaction" cannot serialize for Web Socket.
        public Transaction Transaction { get; }

        private string transactionId;

        /// <summary>
        /// Makes the transaction ID available for Web Socket consumers.
        /// </summary>
        public string TransactionId
        {
            get
            {
                return this.transactionId ??= this.Transaction.ToString();
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionBroadcastState TransactionBroadcastState { get; set; }

        public string ErrorMessage { get; set; }

        public bool CanRespondToGetData { get; set; }

        public BroadcastTransactionStateChanedEntry(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.TransactionBroadcastState = transactionBroadcastState;
            this.ErrorMessage = (errorMessage == null) ? string.Empty : errorMessage;
        }
    }
}