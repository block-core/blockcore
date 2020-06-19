using System.Collections.Generic;
using System.Linq;
using Blockcore.Base;
using Blockcore.Consensus;
using Blockcore.Controllers;
using Blockcore.Controllers.Models;
using Blockcore.Features.RPC;
using Blockcore.Features.RPC.Exceptions;
using Blockcore.Interfaces;
using Blockcore.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Api.Controllers
{
    /// <summary>
    /// Controller providing RPC operations on a watch-only wallet.
    /// </summary>
    public class BlockStoreRPCController : FeatureController
    {
        /// <summary>Full Node.</summary>
        private readonly IFullNode fullNode;

        /// <summary>Thread safe access to the best chain of block headers from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>Specification of the network the node runs on.</summary>
        private readonly Network network;

        /// <summary>Wallet broadcast manager.</summary>
        private readonly IBroadcasterManager broadcasterManager;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Provides access to the block store database.</summary>
        private readonly IBlockStore blockStore;

        /// <summary>Information about node's chain.</summary>
        private readonly IChainState chainState;

        /// <inheritdoc />
        public BlockStoreRPCController(
            IFullNode fullNode,
            IConsensusManager consensusManager,
            ChainIndexer chainIndexer,
            Network network,
            ILoggerFactory loggerFactory,
            IBlockStore blockStore,
            IChainState chainState) : base(fullNode: fullNode, consensusManager: consensusManager, chainIndexer: chainIndexer, network: network)
        {
            this.fullNode = fullNode;
            this.chainIndexer = chainIndexer;
            this.network = network;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.blockStore = blockStore;
            this.chainState = chainState;
        }

        /// <summary>
        /// By default this function only works when there is an unspent output in the utxo for this transaction.
        /// To make it work, you need to maintain a transaction index, using the -txindex command line option.
        /// </summary>
        /// <param name="txids">The txids to filter</param>
        /// <param name="blockhash">If specified, looks for txid in the block with this hash</param>
        /// <returns></returns>
        [ActionName("gettxoutproof")]
        [ActionDescription("Checks if transactions are within block. Returns proof of transaction inclusion (raw MerkleBlock).")]
        public MerkleBlock GetTxOutProof(string[] txids, string blockhash = "")
        {
            List<uint256> transactionIds = new List<uint256>();
            Block block = null;
            foreach (var txString in txids)
            {
                transactionIds.Add(uint256.Parse(txString));
            }

            if (!string.IsNullOrEmpty(blockhash))
            {
                // Loop through txids and veryify that the transaction is in the block.
                foreach (var transactionId in transactionIds)
                {
                    var hashBlock = uint256.Parse(blockhash);
                    ChainedHeader chainedHeader = this.GetTransactionBlock(transactionId, this.fullNode, this.chainIndexer);
                    if (chainedHeader.HashBlock != hashBlock)
                    {
                        throw new RPCServerException(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, "Not all transactions found in specified or retrieved block");
                    }
                    if (block == null && chainedHeader.BlockDataAvailability == BlockDataAvailabilityState.BlockAvailable) // Only get the block once.
                    {
                        block = this.blockStore.GetBlock(chainedHeader.HashBlock);
                    }
                }
            }
            else
            {
                // Loop through txids and try to find which block they're in. Exit loop once a block is found.
                foreach (var transactionId in transactionIds)
                {
                    ChainedHeader chainedHeader = this.GetTransactionBlock(transactionId, this.fullNode, this.chainIndexer);
                    if (chainedHeader.BlockDataAvailability == BlockDataAvailabilityState.BlockAvailable)
                    {
                        block = this.blockStore.GetBlock(chainedHeader.HashBlock);
                        break;
                    }
                }

            }
            if (block == null)
            {
                throw new RPCServerException(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, "Block not found");
            }
            var result = new MerkleBlock(block, transactionIds.ToArray());
            return result;
        }

        internal ChainedHeader GetTransactionBlock(uint256 trxid, IFullNode fullNode, ChainIndexer chain)
        {
            Guard.NotNull(fullNode, nameof(fullNode));

            ChainedHeader block = null;
            uint256 blockid = this.blockStore?.GetBlockIdByTransactionId(trxid);
            if (blockid != null)
            {
                block = chain?.GetHeader(blockid);
            }
            return block;
        }
    }
}
