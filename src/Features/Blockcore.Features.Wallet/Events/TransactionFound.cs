using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus.Transaction;
using Blockcore.EventBus;
using NBitcoin;

namespace Blockcore.Features.Wallet.Events
{
    /// <summary>
    /// Event that is executed when a transaction is found in the wallet.
    /// </summary>
    /// <seealso cref="Blockcore.EventBus.EventBase" />
    public class TransactionFound : EventBase
    {
        public Transaction FoundTransaction { get; }

        public TransactionFound(Transaction foundTransaction)
        {
            this.FoundTransaction = foundTransaction;
        }
    }
}
