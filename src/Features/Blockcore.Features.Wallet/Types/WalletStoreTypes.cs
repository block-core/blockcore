using System;
using System.Collections.Generic;
using LiteDB;

namespace Blockcore.Features.Wallet.Types
{
    public class TrxOutputSlim
    {
        [BsonId]
        public string Utxo { get; set; }

        public string Address { get; set; }

        public long Amount { get; set; }

        public bool? IsCoinBase { get; set; }

        public bool? IsCoinStake { get; set; }

        public bool? IsColdCoinStake { get; set; }

        public int? BlockHeight { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public string ScriptPubKey { get; set; }

        public bool? IsPropagated { get; set; }
    }

    public class TrxOutput
    {
        [BsonId]
        public string Utxo { get; set; }

        public string Address { get; set; }

        public long Amount { get; set; }

        public bool? IsCoinBase { get; set; }

        public bool? IsCoinStake { get; set; }

        public bool? IsColdCoinStake { get; set; }

        public int? BlockHeight { get; set; }

        public string BlockHash { get; set; }

        public int? IndexInBlock { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public string MerkleProofHex { get; set; }

        public string ScriptPubKey { get; set; }

        public string TransactionHex { get; set; }

        public bool? IsPropagated { get; set; }

        public TrxSpentOutput SpendingDetails { get; set; }
    }

    public class TrxSpentOutput
    {
        public TrxSpentOutput()
        {
            this.Payments = new List<TrxSpentOutputPayment>();
        }

        public string TransactionId { get; set; }

        public ICollection<TrxSpentOutputPayment> Payments { get; set; }

        public int? BlockHeight { get; set; }

        public int? BlockIndex { get; set; }

        public bool? IsCoinStake { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public string SpentTrxHex { get; set; }
    }

    public class TrxSpentOutputPayment
    {
        public string DestinationScriptPubKey { get; set; }

        public string DestinationAddress { get; set; }

        public int? OutputIndex { get; set; }

        public long Amount { get; set; }
    }
}