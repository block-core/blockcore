using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Blockcore.Features.Miner.Api.Models
{
    public class GetStakingAddressesModel
    {
        [JsonProperty(PropertyName = "addresses")]
        public IList<GetStakingAddressesModelItem> Addresses { get; set; }
    }

    public class RedeemScriptExpiryItem
    {
        /// <summary>
        /// A script that is used for P2SH and P2WSH scenarios (mostly used for staking).
        /// </summary>
        [JsonProperty(PropertyName = "redeemScript")]
        public string RedeemScript { get; set; }

        /// <summary>
        /// Specify whether UTXOs associated with this address is within the allowed staking time.
        /// </summary>
        [JsonProperty(PropertyName = "expiry")]
        public DateTime? StakingExpiry { get; set; }

        [JsonProperty(PropertyName = "expired")]
        public bool Expired { get; set; }
    }

    public class GetStakingAddressesModelItem
    {
        [JsonProperty(PropertyName = "address")]
        public string Addresses { get; set; }

        [JsonProperty(PropertyName = "expiry")]
        public DateTime? Expiry { get; set; }

        [JsonProperty(PropertyName = "expired")]
        public bool Expired { get; set; }

        [JsonProperty(PropertyName = "redeemScriptsExpiry")]
        public ICollection<RedeemScriptExpiryItem> RedeemScriptExpiry { get; set; } = new List<RedeemScriptExpiryItem>();
    }
}
