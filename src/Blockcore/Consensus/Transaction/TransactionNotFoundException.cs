﻿using System;
using NBitcoin;

namespace Blockcore.Consensus.Transaction
{
    public class TransactionNotFoundException : Exception
    {
        public TransactionNotFoundException()
        {
        }
        public TransactionNotFoundException(uint256 txId)
            : this(null, txId, null)
        {

        }
        public TransactionNotFoundException(string message, uint256 txId)
            : this(message, txId, null)
        {
        }
        public TransactionNotFoundException(string message, uint256 txId, Exception inner)
            : base(message ?? "Transaction " + txId + " not found", inner)
        {
            this.TxId = txId;
        }
        public uint256 TxId
        {
            get;
            set;
        }
    }
}
