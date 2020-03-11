using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Stratis.Bitcoin.Features.Miner.Models
{
    /// <summary>
    /// Base model for requests.
    /// </summary>
    public class RequestModel
    {
        /// <summary>
        /// Creates a JSON serialized object.
        /// </summary>
        /// <returns>A JSON serialized object.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    /// <summary>
    /// Model for the "startstaking" request.
    /// </summary>
    public class StartStakingRequest : RequestModel
    {
        /// <summary>
        /// The wallet password.
        /// </summary>
        [Required(ErrorMessage = "A password is required.")]
        public string Password { get; set; }

        /// <summary>
        /// The wallet name.
        /// </summary>
        [Required(ErrorMessage = "The name of the wallet is missing.")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Model for the "generate" mining request.
    /// </summary>
    public class MiningRequest : RequestModel
    {
        /// <summary>
        /// Number of blocks to mine.
        /// </summary>
        [Required(ErrorMessage = "The number of blocks to mine required.")]
        public int BlockCount { get; set; }
    }

    /// <summary>
    /// Model for the staking request.
    /// </summary>
    public class StakingExpiryRequest : RequestModel
    {
        public StakingExpiryRequest()
        {
            this.StakingExpiry = DateTime.UtcNow;
        }

        /// <summary>
        /// Name of wallet.
        /// </summary>
        [Required(ErrorMessage = "Name of wallet.")]
        public string WalletName { get; set; }

        /// <summary>
        /// Address to change.
        /// </summary>
        [Required(ErrorMessage = "Address to change.")]
        public string Address { get; set; }

        /// <summary>
        /// Specify whether UTXOs associated with this address is within the allowed staing time, null will disable staking. 
        /// </summary>
        [JsonProperty(PropertyName = "stakingExpiry", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime? StakingExpiry { get; set; }
    }

    public class StakingNotExpiredRequest : RequestModel
    {
        /// <summary>
        /// Name of wallet.
        /// </summary>
        [Required(ErrorMessage = "Name of wallet.")]
        public string WalletName { get; set; }

        public bool Segwit { get; set; }
    }
}
