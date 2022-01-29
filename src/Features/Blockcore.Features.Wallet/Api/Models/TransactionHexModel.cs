using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Api.Models
{

    public class TransactionHexModel
    {
        /// <summary>The transaction bytes as a hexadecimal string.</summary>
        [JsonProperty(PropertyName = "transactionHex")]
        public string TransactionHex { get; set; }

        
        public override string ToString()
        {
            return this.TransactionHex;
        }
    }
}
