using System;
using System.Collections.Generic;
using Blockcore.Consensus.ScriptInfo;
using NBitcoin;

namespace Blockcore.Features.Wallet.Database
{
    public class PaymentDetails
    {
        public Script DestinationScriptPubKey { get; set; }

        public string DestinationAddress { get; set; }

        public int? OutputIndex { get; set; }

        public Money Amount { get; set; }

        public bool? PayToSelf { get; set; }
    }

    public class SpendingDetails
    {
        public SpendingDetails()
        {
            this.Payments = new List<PaymentDetails>();
        }

        public uint256 TransactionId { get; set; }

        public ICollection<PaymentDetails> Payments { get; set; }

        public int? BlockHeight { get; set; }

        public int? BlockIndex { get; set; }

        public bool? IsCoinStake { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public string Hex { get; set; }

        public bool IsSpentConfirmed()
        {
            return this.BlockHeight != null;
        }
    }
}