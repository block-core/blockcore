using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.EventBus;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Events
{
    /// <summary>
    /// Event that is executed when a transaction is found in the wallet.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class TransactionFound : EventBase
    {
        [JsonIgnore]
        public Transaction FoundTransaction { get; }

        public string TransactionHex { get; set; }

        public string TransactionId { get; set; }

        public TransactionFound(Transaction foundTransaction)
        {
            this.FoundTransaction = foundTransaction;
            this.TransactionHex = foundTransaction.ToHex();
            this.TransactionId = foundTransaction.ToString();
        }
    }
}
