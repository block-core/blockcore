using System;
using NBitcoin;

namespace Blockcore.EventBus.CoreEvents
{
    /// <summary>
    /// Event that is executed when a transaction is received from another peer.
    /// </summary>
    /// <seealso cref="EventBase" />
    public class TransactionReceived : EventBase
    {
        public Transaction ReceivedTransaction { get; }

        public TransactionReceived(Transaction receivedTransaction)
        {
            this.ReceivedTransaction = receivedTransaction;
        }
    }
}