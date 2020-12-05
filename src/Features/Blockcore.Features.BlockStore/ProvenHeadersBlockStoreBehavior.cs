using System.Collections.Generic;
using System.Linq;
using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.BlockStore
{
    /// <inheritdoc />
    public class ProvenHeadersBlockStoreBehavior : BlockStoreBehavior
    {
        private readonly Network network;
        private readonly ICheckpoints checkpoints;

        public ProvenHeadersBlockStoreBehavior(Network network, ChainIndexer chainIndexer, IChainState chainState, ILoggerFactory loggerFactory, IConsensusManager consensusManager, ICheckpoints checkpoints, IBlockStoreQueue blockStoreQueue)
            : base(chainIndexer, chainState, loggerFactory, consensusManager, blockStoreQueue)
        {
            this.network = Guard.NotNull(network, nameof(network));
            this.checkpoints = Guard.NotNull(checkpoints, nameof(checkpoints));
        }

        /// <inheritdoc />
        /// <returns>The <see cref="HeadersPayload"/> instance to announce to the peer, or <see cref="ProvenHeadersPayload"/> if the peers requires it.</returns>
        protected override Payload BuildHeadersAnnouncePayload(IEnumerable<ChainedHeader> headers)
        {
            // Sanity check. That should never happen.
            if (!headers.All(x => x.ProvenBlockHeader != null))
                throw new BlockStoreException("UnexpectedError: BlockHeader is expected to be a ProvenBlockHeader");

            var provenHeadersPayload = new ProvenHeadersPayload(headers.Select(s => s.ProvenBlockHeader).ToArray());

            return provenHeadersPayload;
        }

        public override object Clone()
        {
            var res = new ProvenHeadersBlockStoreBehavior(this.network, this.ChainIndexer, this.chainState, this.loggerFactory, this.consensusManager, this.checkpoints, this.blockStoreQueue)
            {
                CanRespondToGetBlocksPayload = this.CanRespondToGetBlocksPayload,
                CanRespondToGetDataPayload = this.CanRespondToGetDataPayload
            };

            return res;
        }
    }
}