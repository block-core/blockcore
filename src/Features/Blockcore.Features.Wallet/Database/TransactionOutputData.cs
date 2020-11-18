using System;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Types;
using Blockcore.Utilities.JsonConverters;
using LiteDB;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Database
{
    /// <summary>
    /// An object containing transaction data.
    /// </summary>
    public class TransactionOutputData
    {
        /// <summary>
        /// Transaction id.
        /// </summary>
        [JsonProperty(PropertyName = "outPoint")]
        [JsonConverter(typeof(OutPointJsonConverter))]
        [BsonId]
        public OutPoint OutPoint { get; set; }

        /// <summary>
        /// The address (hash of P2PKH) representation of the pubkey this utxo is associated to.
        /// This is only the unique way to identify the pubkey, the utxo itself can be any of the script representations
        /// </summary>
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        /// <summary>
        /// The HD account the address belongs to.
        /// </summary>
        [JsonProperty(PropertyName = "accountIndex")]
        public int AccountIndex { get; set; }

        /// <summary>
        /// Transaction id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [JsonConverter(typeof(UInt256JsonConverter))]
        public uint256 Id { get; set; }

        /// <summary>
        /// The transaction amount.
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        [JsonConverter(typeof(MoneyJsonConverter))]
        public Money Amount { get; set; }

        /// <summary>
        /// A value indicating whether this is a coinbase transaction or not.
        /// </summary>
        [JsonProperty(PropertyName = "isCoinBase", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsCoinBase { get; set; }

        /// <summary>
        /// A value indicating whether this is a coinstake transaction or not.
        /// </summary>
        [JsonProperty(PropertyName = "isCoinStake", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsCoinStake { get; set; }

        /// <summary>
        /// A value indicating whether this is a coldstake transaction or not.
        /// </summary>
        [JsonProperty(PropertyName = "isColdCoinStake", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsColdCoinStake { get; set; }

        /// <summary>
        /// The index of this scriptPubKey in the transaction it is contained.
        /// </summary>
        /// <remarks>
        /// This is effectively the index of the output, the position of the output in the parent transaction.
        /// </remarks>
        [JsonProperty(PropertyName = "index", NullValueHandling = NullValueHandling.Ignore)]
        public int Index { get; set; }

        /// <summary>
        /// The height of the block including this transaction.
        /// </summary>
        [JsonProperty(PropertyName = "blockHeight", NullValueHandling = NullValueHandling.Ignore)]
        public int? BlockHeight { get; set; }

        /// <summary>
        /// The hash of the block including this transaction.
        /// </summary>
        [JsonProperty(PropertyName = "blockHash", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(UInt256JsonConverter))]
        public uint256 BlockHash { get; set; }

        /// <summary>
        /// The index of this transaction in the block in which it is contained.
        /// </summary>
        [JsonProperty(PropertyName = "blockIndex", NullValueHandling = NullValueHandling.Ignore)]
        public int? BlockIndex { get; set; }

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        [JsonProperty(PropertyName = "creationTime")]
        [JsonConverter(typeof(DateTimeOffsetConverter))]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the Merkle proof for this transaction.
        /// </summary>
        [JsonProperty(PropertyName = "merkleProof", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(BitcoinSerializableJsonConverter))]
        public PartialMerkleTree MerkleProof { get; set; }

        /// <summary>
        /// The script pub key for this address.
        /// </summary>
        [JsonProperty(PropertyName = "scriptPubKey")]
        [JsonConverter(typeof(ScriptJsonConverter))]
        public Script ScriptPubKey { get; set; }

        /// <summary>
        /// Hexadecimal representation of this transaction.
        /// </summary>
        [JsonProperty(PropertyName = "hex", NullValueHandling = NullValueHandling.Ignore)]
        public string Hex { get; set; }

        /// <summary>
        /// Propagation state of this transaction.
        /// </summary>
        /// <remarks>Assume it's <c>true</c> if the field is <c>null</c>.</remarks>
        [JsonProperty(PropertyName = "isPropagated", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPropagated { get; set; }

        /// <summary>
        /// The details of the transaction in which the output referenced in this transaction is spent.
        /// </summary>
        [JsonProperty(PropertyName = "spendingDetails", NullValueHandling = NullValueHandling.Ignore)]
        public SpendingDetails SpendingDetails { get; set; }

        /// <summary>
        /// Determines whether this transaction is confirmed.
        /// </summary>
        public bool IsConfirmed()
        {
            return this.BlockHeight != null;
        }

        /// <summary>
        /// Indicates whether an output has been spent.
        /// </summary>
        /// <returns>A value indicating whether an output has been spent.</returns>
        public bool IsSpent()
        {
            return this.SpendingDetails != null;
        }

        /// <summary>
        /// Checks if the output is not spent, with the option to choose whether only confirmed ones are considered.
        /// </summary>
        /// <param name="confirmedOnly">A value indicating whether we only want confirmed amount.</param>
        /// <returns>The total amount that has not been spent.</returns>
        public Money GetUnspentAmount(bool confirmedOnly)
        {
            // The spendable balance is 0 if the output is spent or it needs to be confirmed to be considered.
            if (this.IsSpent() || (confirmedOnly && !this.IsConfirmed()))
            {
                return Money.Zero;
            }

            return this.Amount;
        }
    }
}