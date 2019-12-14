using System.Collections.Generic;
using NBitcoin;
using Newtonsoft.Json;

namespace Stratis.Bitcoin.Features.Miner.Models
{
    public class GetStakingAddressesModel
    {
        [JsonProperty(PropertyName = "Addresses")]
        public IList<string> Addresses { get; set; }
    }
}
