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
        [JsonIgnore] // The "Transaction" cannot serialize for Web Socket.
        public Transaction ReceivedTransaction { get; }

        private string transactionId;

        /// <summary>
        /// Makes the transaction ID available for Web Socket consumers.
        /// </summary>
        public string TransactionId
        {
            get
            {
                return this.transactionId ??= this.ReceivedTransaction.ToString();
            }
        }

        public TransactionReceived(Transaction receivedTransaction)
        {
            this.ReceivedTransaction = receivedTransaction;
        }
    }
}