using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Controllers;
using Blockcore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus
{
    /// <summary>
    /// A <see cref="FeatureController"/> that provides API and RPC methods from the consensus loop.
    /// </summary>
    public class ConsensusRPCController : FeatureController
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public ConsensusRPCController(
            ILoggerFactory loggerFactory,
            IChainState chainState,
            IConsensusManager consensusManager,
            ChainIndexer chainIndexer)
            : base(chainState: chainState, consensusManager: consensusManager, chainIndexer: chainIndexer)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(chainIndexer, nameof(chainIndexer));
            Guard.NotNull(chainState, nameof(chainState));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        /// Implements the getbestblockhash RPC call.
        /// </summary>
        /// <returns>A <see cref="uint256"/> hash of the block at the consensus tip.</returns>
        [ActionName("getbestblockhash")]
        [ActionDescription("Get the hash of the block at the consensus tip.")]
        public uint256 GetBestBlockHash()
        {
            return this.ChainState.ConsensusTip?.HashBlock;
        }

        /// <summary>
        /// Implements the getblockhash RPC call.
        /// </summary>
        /// <param name="height">The requested block height.</param>
        /// <returns>A <see cref="uint256"/> hash of the block at the given height. <c>Null</c> if block not found.</returns>
        [ActionName("getblockhash")]
        [ActionDescription("Gets the hash of the block at the given height.")]
        public uint256 GetBlockHash(int height)
        {
            this.logger.LogDebug("GetBlockHash {0}", height);

            uint256 bestBlockHash = this.ConsensusManager.Tip?.HashBlock;
            ChainedHeader bestBlock = bestBlockHash == null ? null : this.ChainIndexer.GetHeader(bestBlockHash);
            if (bestBlock == null)
                return null;
            ChainedHeader block = this.ChainIndexer.GetHeader(height);
            uint256 hash = block == null || block.Height > bestBlock.Height ? null : block.HashBlock;

            if (hash == null)
                throw new BlockNotFoundException($"No block found at height {height}");

            return hash;
        }
    }
}
