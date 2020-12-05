using System.Collections.Generic;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Controllers;
using Blockcore.Features.RPC;
using Blockcore.Features.RPC.Exceptions;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Api.Controllers
{
    /// <summary>
    /// Controller providing RPC operations on a watch-only wallet.
    /// </summary>
    public class BlockStoreRPCController : FeatureController
    {
        /// <summary>Consensus manager class.</summary>
        private readonly IConsensusManager consensusManager;

        /// <summary>Thread safe access to the best chain of block headers from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>Provides access to the block store database.</summary>
        private readonly IBlockStore blockStore;

        /// <inheritdoc />
        public BlockStoreRPCController(
            IFullNode fullNode,
            IConsensusManager consensusManager,
            ChainIndexer chainIndexer,
            Network network,
            IBlockStore blockStore) : base(fullNode: fullNode, consensusManager: consensusManager, chainIndexer: chainIndexer, network: network)
        {
            this.consensusManager = consensusManager;
            this.chainIndexer = chainIndexer;
            this.blockStore = blockStore;
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
            ChainedHeaderBlock block = null;
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
                    ChainedHeader chainedHeader = this.GetTransactionBlock(transactionId);
                    if (chainedHeader.HashBlock != hashBlock)
                    {
                        throw new RPCServerException(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, "Not all transactions found in specified or retrieved block");
                    }
                    if (block == null && chainedHeader.BlockDataAvailability == BlockDataAvailabilityState.BlockAvailable) // Only get the block once.
                    {
                        block = this.consensusManager.GetBlockData(chainedHeader.HashBlock);
                    }
                }
            }
            else
            {
                // Loop through txids and try to find which block they're in. Exit loop once a block is found.
                foreach (var transactionId in transactionIds)
                {
                    ChainedHeader chainedHeader = this.GetTransactionBlock(transactionId);
                    if (chainedHeader.BlockDataAvailability == BlockDataAvailabilityState.BlockAvailable)
                    {
                        block = this.consensusManager.GetBlockData(chainedHeader.HashBlock);
                        break;
                    }
                }
            }
            if (block == null)
            {
                throw new RPCServerException(RPCErrorCode.RPC_INVALID_ADDRESS_OR_KEY, "Block not found");
            }
            var result = new MerkleBlock(block.Block, transactionIds.ToArray());
            return result;
        }

        internal ChainedHeader GetTransactionBlock(uint256 trxid)
        {
            ChainedHeader block = null;
            uint256 blockid = this.blockStore?.GetBlockIdByTransactionId(trxid);
            if (blockid != null)
            {
                block = this.chainIndexer?.GetHeader(blockid);
            }
            return block;
        }
    }
}
