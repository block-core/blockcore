using System;
using Blockcore.Consensus.TransactionInfo;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcastTransactionStateChanedEntry
    {
        [JsonIgnore]
        public Transaction Transaction { get; }

        public string TransactionHex { get; }

        public string TransactionId { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionBroadcastState TransactionBroadcastState { get; set; }

        public string ErrorMessage { get; private set; }

        public bool CanRespondToGetData { get; set; }

        public BroadcastTransactionStateChanedEntry(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage)
        {
            this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            this.TransactionHex = transaction.ToHex();
            this.TransactionId = transaction.ToString();
            this.TransactionBroadcastState = transactionBroadcastState;
            this.ErrorMessage = (errorMessage == null) ? string.Empty : errorMessage;
        }
    }
}