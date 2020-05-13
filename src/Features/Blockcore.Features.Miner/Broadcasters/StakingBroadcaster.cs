using System.Collections.Generic;
using Blockcore.AsyncWork;
using Blockcore.Broadcasters;
using Blockcore.EventBus;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.Miner.Broadcasters
{
    /// <summary>
    /// Broadcasts current staking information to Web Socket clients
    /// </summary>
    public class StakingBroadcaster : ClientBroadcasterBase
    {
        private readonly IPosMinting posMinting;

        public StakingBroadcaster(
            ILoggerFactory loggerFactory,
            IPosMinting posMinting,
            INodeLifetime nodeLifetime,
            IAsyncProvider asyncProvider,
            IEventsSubscriptionService eventsHub)
            : base(eventsHub, loggerFactory, nodeLifetime, asyncProvider)
        {
            this.posMinting = posMinting;
        }

        protected override IEnumerable<EventBase> GetMessages()
        {
            if (null != this.posMinting)
            {
                yield return new StakingInfoClientEvent(this.posMinting.GetGetStakingInfoModel());
            }
        }
    }
}