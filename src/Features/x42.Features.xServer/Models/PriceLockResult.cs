using Newtonsoft.Json;

namespace x42.Features.xServer.Models
{
    public class PriceLockResult
    {
        [JsonProperty(Order = 1, PropertyName = "success")]
        public bool Success { get; set; }

        [JsonProperty(Order = 2, PropertyName = "resultMessage")]
        public string ResultMessage { get; set; }

        [JsonProperty(Order = 3, PropertyName = "priceLockId")]
        public string PriceLockId { get; set; }

        [JsonProperty(Order = 4, PropertyName = "status")]
        public int Status { get; set; }

        [JsonProperty(Order = 5, PropertyName = "requestAmount")]
        public decimal RequestAmount { get; set; }

        [JsonProperty(Order = 6, PropertyName = "requestAmountPair")]
        public int RequestAmountPair { get; set; }

        [JsonProperty(Order = 7, PropertyName = "feeAmount")]
        public decimal FeeAmount { get; set; }

        [JsonProperty(Order = 8, PropertyName = "feeAddress")]
        public string FeeAddress { get; set; }

        [JsonProperty(Order = 9, PropertyName = "destinationAmount")]
        public decimal DestinationAmount { get; set; }

        [JsonProperty(Order = 10, PropertyName = "destinationAddress")]
        public string DestinationAddress { get; set; }

        [JsonProperty(Order = 11, PropertyName = "transactionId")]
        public string TransactionId { get; set; }

        [JsonProperty(Order = 12, PropertyName = "signAddress")]
        public string SignAddress { get; set; }

        [JsonProperty(Order = 13, PropertyName = "priceLockSignature")]
        public string PriceLockSignature { get; set; }

        [JsonProperty(Order = 14, PropertyName = "payeeSignature")]
        public string PayeeSignature { get; set; }

        [JsonProperty(Order = 15, PropertyName = "expireBlock")]
        public int ExpireBlock { get; set; }
    }
}
