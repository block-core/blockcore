using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Features.BlockStore;
using Blockcore.Features.PoA.Payloads;
using Blockcore.Interfaces;
using Blockcore.P2P.Protocol.Payloads;

namespace Blockcore.Features.PoA.Behaviors
{
    public class PoABlockStoreBehavior : BlockStoreBehavior
    {
        public PoABlockStoreBehavior(ChainIndexer chainIndexer, IChainState chainState, ILoggerFactory loggerFactory, IConsensusManager consensusManager, IBlockStoreQueue blockStoreQueue)
            : base(chainIndexer, chainState, loggerFactory, consensusManager, blockStoreQueue)
        {
        }

        /// <inheritdoc />
        protected override Payload BuildHeadersAnnouncePayload(IEnumerable<BlockHeader> headers)
        {
            var poaHeaders = headers.Cast<PoABlockHeader>().ToList();

            return new PoAHeadersPayload(poaHeaders);
        }

        public override object Clone()
        {
            var res = new PoABlockStoreBehavior(this.ChainIndexer, this.chainState, this.loggerFactory, this.consensusManager, this.blockStoreQueue)
            {
                CanRespondToGetBlocksPayload = this.CanRespondToGetBlocksPayload,
                CanRespondToGetDataPayload = this.CanRespondToGetDataPayload
            };

            return res;
        }
    }
}
