using System;
using Blockcore.EventBus;
using Blockcore.EventBus.CoreEvents;

namespace Blockcore.Features.WebHost.Events
{
    public class TransactionReceivedClientEvent : IClientEvent
    {
        public string TxHash { get; set; }

        public bool IsCoinbase { get; set; }

        public bool IsCoinstake { get; set; }

        public uint Time { get; set; }

        public Type NodeEventType { get; } = typeof(TransactionReceived);

        public void BuildFrom(EventBase @event)
        {
            if (@event is TransactionReceived transactionReceived)
            {
                this.TxHash = transactionReceived.ReceivedTransaction.GetHash().ToString();
                this.IsCoinbase = transactionReceived.ReceivedTransaction.IsCoinBase;
                this.IsCoinstake = transactionReceived.ReceivedTransaction.IsCoinStake;
               // this.Time = transactionReceived.ReceivedTransaction.Time;
                return;
            }

            throw new ArgumentException();
        }
    }
}