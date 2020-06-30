using Blockcore.Features.Wallet.Api.Models;
using Newtonsoft.Json;

namespace Blockcore.Features.WalletWatchOnly.Api.Models
{
    public class ListTransactionsModel
    {
        /// <summary>
        /// Only returns true if imported addresses were involved in transaction.
        /// </summary>
        [JsonProperty("involvesWatchonly")]
        public string InvolvesWatchOnly { get; set; } = "";

        /// <summary>
        /// The bitcoin address of the transaction.
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// The transaction category.
        /// </summary>
        [JsonProperty("category")]
        public ListSinceBlockTransactionCategoryModel Category { get; set; }

        /// <summary>
        /// The total amount received or spent in this transaction.
        /// Can be positive (received), negative (sent) or 0 (payment to yourself).
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// A comment for the address/transaction, if any
        /// </summary>
        [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
        public string Label { get; set; }

        /// <summary>
        /// The amount of the fee in BTC. This is negative and only available for the
        /// 'send' category of transactions.
        /// </summary>
        [JsonProperty("vout", NullValueHandling = NullValueHandling.Ignore)]
        public int VOut { get; set; }

        /// <summary>
        /// The block hash.
        /// </summary>
        /// <summary>
        /// The amount of the fee. This is negative and only available for the 'send' category of transactions.
        /// </summary>
        [JsonProperty("fee", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Fee { get; set; }

        /// <summary>
        /// The number of confirmations for the transaction. Negative confirmations means the
        /// transaction conflicted that many blocks ago.
        /// </summary>
        [JsonProperty("confirmations")]
        public int Confirmations { get; set; }

        /// <summary>
        /// The block hash.
        /// </summary>
        [JsonProperty("blockhash", NullValueHandling = NullValueHandling.Ignore)]
        public string BlockHash { get; set; }

        /// <summary>
        /// The index of the transaction in the block that includes it.
        /// </summary>
        [JsonProperty("blockindex", NullValueHandling = NullValueHandling.Ignore)]
        public int? BlockIndex { get; set; }

        /// <summary>
        /// The time in seconds since epoch (1 Jan 1970 GMT).
        /// </summary>
        [JsonProperty(PropertyName = "blocktime", NullValueHandling = NullValueHandling.Ignore)]
        public long? BlockTime { get; set; }

        /// <summary>
        /// The transaction id.
        /// </summary>
        [JsonProperty("txid")]
        public string TransactionId { get; set; }

        /// <summary>
        /// The transaction time in seconds since epoch (1 Jan 1970 GMT).
        /// </summary>
        [JsonProperty("time")]
        public long TransactionTime { get; set; }

        /// <summary>
        /// The time received in seconds since epoch (1 Jan 1970 GMT).
        /// </summary>
        [JsonProperty(PropertyName = "timereceived")]
        public long TimeReceived { get; set; }
    }
}
