﻿using Blockcore.Consensus.TransactionInfo;
using NBitcoin;

namespace Blockcore.P2P.Protocol.Payloads
{
    /// <summary>
    /// Represents a transaction being sent on the network, is sent after being requested by a getdata (of Transaction or MerkleBlock) message.
    /// </summary>
    [Payload("tx")]
    public class TxPayload : BitcoinSerializablePayload<Transaction>
    {
        public TxPayload()
        {
        }

        public TxPayload(Transaction transaction)
            : base(transaction)
        {
        }
    }
}