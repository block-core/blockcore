using System;
using Blockcore.Broadcasters;
using Blockcore.EventBus;
using Blockcore.Features.Miner.Api.Models;

namespace Blockcore.Features.Miner.Broadcasters
{
    public class StakingInfoClientEvent : EventBase
    {
        public StakingInfoClientEvent(GetStakingInfoModel stakingInfoModel)
        {
            this.StakingInfo = stakingInfoModel;
        }

        public GetStakingInfoModel StakingInfo { get; set; }
    }
}