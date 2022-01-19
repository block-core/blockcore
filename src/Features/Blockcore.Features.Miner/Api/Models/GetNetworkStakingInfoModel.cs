using System;
using Newtonsoft.Json;

namespace Blockcore.Features.Miner.Api.Models
{
    /// <summary>
    /// Data structure returned by RPC command "getstakinginfo".
    /// </summary>
    public class GetNetworkStakingInfoModel
    {
        /// <summary>Target difficulty that the next block must meet.</summary>
        [JsonProperty(PropertyName = "difficulty")]
        public double Difficulty { get; set; }

        /// <summary>Estimation of the total staking weight of all nodes on the network.</summary>
        [JsonProperty(PropertyName = "netStakeWeight")]
        public long NetStakeWeight { get; set; }
      
    }
}
