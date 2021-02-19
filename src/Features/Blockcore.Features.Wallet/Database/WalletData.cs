using System;
using System.Collections.Generic;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Utilities;
using LiteDB;
using NBitcoin;

namespace Blockcore.Features.Wallet.Database
{
    public class WalletBalanceResult
    {
        public long AmountConfirmed { get; set; }
        public long AmountUnconfirmed { get; set; }
    }

    public class WalletData
    {
        [BsonId]
        public string Key { get; set; }

        public string EncryptedSeed { get; set; }

        public string WalletName { get; set; }

        public HashHeightPair WalletTip { get; set; }

        public int WalletVersion { get; set; }

        public ICollection<uint256> BlockLocator { get; set; }
    }

    public class WalletHistoryData
    {
        public bool IsSent { get; set; }
        public ICollection<WalletHistoryData> ReceivedOutputs { get; set; }

        public ICollection<WalletHistoryPaymentData> SentPayments { get; set; }

        public OutPoint OutPoint { get; set; }
        public uint256 SentTo { get; set; }
        public string Address { get; set; }
        public Money Amount { get; set; }
        public bool? IsCoinBase { get; set; }
        public bool? IsCoinStake { get; set; }
        public bool? IsColdCoinStake { get; set; }
        public int? BlockHeight { get; set; }
        public int? BlockIndex { get; set; }
        public DateTimeOffset CreationTime { get; set; }
        public Script ScriptPubKey { get; set; }
    }

    public class WalletHistoryPaymentData
    {
        public string DestinationAddress { get; set; }

        public Money Amount { get; set; }
        public bool? PayToSelf { get; set; }
    }

    public class TransactionData
    {
        public TransactionData()
        {
            this.SpendingDetailsPayments = new List<PaymentDetails>();
        }

        public OutPoint OutPoint { get; set; }

        public string Address { get; set; }

        public int AccountIndex { get; set; }

        public uint256 Id { get; set; }

        public Money Amount { get; set; }

        public bool? IsCoinBase { get; set; }

        public bool? IsCoinStake { get; set; }

        public bool? IsColdCoinStake { get; set; }

        public int IndexInTransaction { get; set; }

        public int? BlockHeight { get; set; }

        public uint256 BlockHash { get; set; }

        public int? BlockIndex { get; set; }

        public DateTimeOffset CreationTime { get; set; }

        public PartialMerkleTree MerkleProof { get; set; }
        public string Hex { get; set; }

        public Script ScriptPubKey { get; set; }

        public bool? IsPropagated { get; set; }

        public uint256 SpendingDetailsTransactionId { get; set; }

        public ICollection<PaymentDetails> SpendingDetailsPayments { get; set; }

        public int? SpendingDetailsBlockHeight { get; set; }

        public int? SpendingDetailsBlockIndex { get; set; }

        public bool? SpendingDetailsIsCoinStake { get; set; }

        public DateTimeOffset? SpendingDetailsCreationTime { get; set; }

        public string SpendingDetailsHex { get; set; }
    }
}