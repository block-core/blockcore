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
        [JsonIgnore] // The "Transaction" cannot serialize for Web Socket.
        public Transaction FoundTransaction { get; }

        private string transactionId;

        /// <summary>
        /// Makes the transaction ID available for Web Socket consumers.
        /// </summary>
        public string TransactionId
        {
            get
            {
                return this.transactionId ??= this.FoundTransaction.ToString();
            }
        }

        public TransactionFound(Transaction foundTransaction)
        {
            this.FoundTransaction = foundTransaction;
        }
    }
}
