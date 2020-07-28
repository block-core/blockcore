using System.Collections.Generic;
using Blockcore.Utilities.JsonConverters;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Api.Models
{
    public class WalletBuildTransactionModel
    {
        [JsonProperty(PropertyName = "fee")]
        public Money Fee { get; set; }

        [JsonProperty(PropertyName = "hex")]
        public string Hex { get; set; }

        [JsonProperty(PropertyName = "transactionId")]
        [JsonConverter(typeof(UInt256JsonConverter))]
        public uint256 TransactionId { get; set; }

        [JsonProperty(PropertyName = "inputAddress")]
        public string InputAddress { get; set; }
    }
}
