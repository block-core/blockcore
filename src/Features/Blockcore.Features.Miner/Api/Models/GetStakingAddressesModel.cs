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

    public class GetStakingAddressesModelItem
    {
        [JsonProperty(PropertyName = "address")]
        public string Addresses { get; set; }

        [JsonProperty(PropertyName = "expiry")]
        public DateTime? Expiry { get; set; }
    }
}
