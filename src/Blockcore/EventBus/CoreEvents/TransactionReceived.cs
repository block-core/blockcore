using System;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// Event that is executed when a transaction is received from another peer.
    /// </summary>
    /// <seealso cref="EventBase" />
    public class TransactionReceived : EventBase
    {
        [JsonIgnore]
        public Transaction ReceivedTransaction { get; }

        public string TransactionHex { get; set; }

        public string TransactionId { get; set; }

        public TransactionReceived(Transaction receivedTransaction)
        {
            this.ReceivedTransaction = receivedTransaction;
            this.TransactionHex = receivedTransaction.ToHex();
            this.TransactionId = receivedTransaction.ToString();
        }
    }
}