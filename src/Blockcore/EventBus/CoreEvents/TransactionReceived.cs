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

        public string TxHash { get; set; }

        public bool IsCoinbase { get; set; }

        public bool IsCoinstake { get; set; }

        public TransactionReceived(Transaction receivedTransaction)
        {
            this.ReceivedTransaction = receivedTransaction;

            this.TxHash = this.ReceivedTransaction.GetHash().ToString();
            this.IsCoinbase = this.ReceivedTransaction.IsCoinBase;
            this.IsCoinstake = this.ReceivedTransaction.IsCoinStake;
        }
    }
}