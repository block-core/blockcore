using System.Collections.Generic;
using Blockcore.AsyncWork;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.SignalR.Events;
using Blockcore.Features.SignalR.Hubs;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.SignalR.Broadcasters
{
    /// <summary>
    /// Broadcasts current staking information to SignalR clients
    /// </summary>
    public class StakingBroadcaster : ClientBroadcasterBase
    {
        private readonly IPosMinting posMinting;

        public StakingBroadcaster(
            ILoggerFactory loggerFactory,
            IPosMinting posMinting,
            INodeLifetime nodeLifetime,
            IAsyncProvider asyncProvider,
            EventsHub eventsHub)
            : base(eventsHub, loggerFactory, nodeLifetime, asyncProvider)
        {
            this.posMinting = posMinting;
        }

        protected override IEnumerable<IClientEvent> GetMessages()
        {
            if (null != this.posMinting)
            {
                yield return new StakingInfoClientEvent(this.posMinting.GetGetStakingInfoModel());
            }
        }
    }
}