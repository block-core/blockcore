﻿using System;
using System.Collections.Generic;
using System.Net;
using Asp.Versioning;
using Blockcore.Base;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Controllers.Models;
using Blockcore.Features.BlockStore.AddressIndexing;
using Blockcore.Features.BlockStore.Api.Models;
using Blockcore.Features.BlockStore.Models;
using Blockcore.Features.Consensus;
using Blockcore.Interfaces;
using Blockcore.NBitcoin;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonErrors;
using Blockcore.Utilities.ModelStateErrors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Blockcore.Features.BlockStore.Api.Controllers
{
    /// <summary>Controller providing operations on a blockstore.</summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class BlockStoreController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly IAddressIndexer addressIndexer;

        /// <summary>Provides access to the block store on disk.</summary>
        private readonly IBlockStore blockStore;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>An interface that provides information about the chain and validation.</summary>
        private readonly IChainState chainState;

        /// <summary>The chain.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>Current network for the active controller instance.</summary>
        private readonly Network network;

        /// <summary>UTXO indexer.</summary>
        private readonly IUtxoIndexer utxoIndexer;

        private readonly IStakeChain stakeChain;

        public BlockStoreController(
            Network network,
            ILoggerFactory loggerFactory,
            IBlockStore blockStore,
            IChainState chainState,
            ChainIndexer chainIndexer,
            IAddressIndexer addressIndexer,
            IUtxoIndexer utxoIndexer,
            IStakeChain stakeChain = null)
        {
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(chainState, nameof(chainState));
            Guard.NotNull(addressIndexer, nameof(addressIndexer));
            Guard.NotNull(utxoIndexer, nameof(utxoIndexer));

            this.addressIndexer = addressIndexer;
            this.network = network;
            this.blockStore = blockStore;
            this.chainState = chainState;
            this.chainIndexer = chainIndexer;
            this.utxoIndexer = utxoIndexer;
            this.stakeChain = stakeChain;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        /// Retrieves the <see cref="addressIndexer"/>'s tip.
        /// </summary>
        /// <returns>An instance of <see cref="AddressIndexerTipModel"/> containing the tip's hash and height.</returns>
        [Route(BlockStoreRouteEndPoint.GetAddressIndexerTip)]
        [HttpGet]
        public IActionResult GetAddressIndexerTip()
        {
            try
            {
                ChainedHeader addressIndexerTip = this.addressIndexer.IndexerTip;
                return this.Json(new AddressIndexerTipModel() { TipHash = addressIndexerTip?.HashBlock, TipHeight = addressIndexerTip?.Height });
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Retrieves the block which matches the supplied block hash.
        /// </summary>
        /// <param name="query">An object containing the necessary parameters to search for a block.</param>
        /// <returns><see cref="BlockModel"/> if block is found, <see cref="NotFoundObjectResult"/> if not found. Returns <see cref="IActionResult"/> with error information if exception thrown.</returns>
        [Route(BlockStoreRouteEndPoint.GetBlock)]
        [HttpGet]
        public IActionResult GetBlock([FromQuery] SearchByHashRequest query)
        {
            if (!this.ModelState.IsValid)
                return ModelStateErrors.BuildErrorResponse(this.ModelState);

            try
            {
                uint256 blockId = uint256.Parse(query.Hash);

                ChainedHeader chainedHeader = this.chainIndexer.GetHeader(blockId);

                if (chainedHeader == null)
                    return this.Ok("Block not found");

                Block block = chainedHeader.Block ?? this.blockStore.GetBlock(blockId);

                // In rare occasions a block that is found in the
                // indexer may not have been pushed to the store yet.
                if (block == null)
                    return this.Ok("Block not found");

                if (!query.OutputJson)
                {
                    return this.Json(block);
                }

                BlockModel blockModel = query.ShowTransactionDetails
                    ? new BlockTransactionDetailsModel(block, chainedHeader, this.chainIndexer.Tip, this.network)
                    : new BlockModel(block, chainedHeader, this.chainIndexer.Tip, this.network);

                if (this.network.Consensus.IsProofOfStake)
                {
                    var posBlock = block as PosBlock;

                    blockModel.PosBlockSignature = posBlock.BlockSignature.ToHex(this.network.Consensus.ConsensusFactory);
                    blockModel.PosBlockTrust = new Target(chainedHeader.GetBlockTarget()).ToUInt256().ToString();
                    blockModel.PosChainTrust = chainedHeader.ChainWork.ToString(); // this should be similar to ChainWork

                    if (this.stakeChain != null)
                    {
                        BlockStake blockStake = this.stakeChain.Get(blockId);

                        blockModel.PosModifierv2 = blockStake?.StakeModifierV2.ToString();
                        blockModel.PosFlags = blockStake?.Flags == BlockFlag.BLOCK_PROOF_OF_STAKE ? "proof-of-stake" : "proof-of-work";
                        blockModel.PosHashProof = blockStake?.HashProof.ToString();
                    }
                }

                return this.Json(blockModel);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets the current consensus tip height.
        /// </summary>
        /// <remarks>This is an API implementation of an RPC call.</remarks>
        /// <returns>The current tip height. Returns <c>null</c> if fails. Returns <see cref="IActionResult"/> with error information if exception thrown.</returns>
        [Route(BlockStoreRouteEndPoint.GetBlockCount)]
        [HttpGet]
        public IActionResult GetBlockCount()
        {
            try
            {
                return this.Json(this.chainState.ConsensusTip.Height);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>Provides balance of the given addresses confirmed with at least <paramref name="minConfirmations"/> confirmations.</summary>
        /// <param name="addresses">A comma delimited set of addresses that will be queried.</param>
        /// <param name="minConfirmations">Only blocks below consensus tip less this parameter will be considered.</param>
        /// <returns>A result object containing the balance for each requested address and if so, a meesage stating why the indexer is not queryable.</returns>
        [Route(BlockStoreRouteEndPoint.GetAddressesBalances)]
        [HttpGet]
        public IActionResult GetAddressesBalances(string addresses, int minConfirmations)
        {
            try
            {
                string[] addressesArray = addresses.Split(',');

                this.logger.LogDebug("Asking data for {0} addresses.", addressesArray.Length);

                AddressBalancesResult result = this.addressIndexer.GetAddressBalances(addressesArray, minConfirmations);

                this.logger.LogDebug("Sending data for {0} addresses.", result.Balances.Count);

                return this.Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>Provides verbose balance data of the given addresses.</summary>
        /// <param name="addresses">A comma delimited set of addresses that will be queried.</param>
        /// <returns>A result object containing the balance for each requested address and if so, a meesage stating why the indexer is not queryable.</returns>
        [Route(BlockStoreRouteEndPoint.GetVerboseAddressesBalances)]
        [HttpGet]
        public IActionResult GetVerboseAddressesBalancesData(string addresses)
        {
            try
            {
                string[] addressesArray = addresses?.Split(',') ?? new string[] { };

                this.logger.LogDebug("Asking data for {0} addresses.", addressesArray.Length);

                VerboseAddressBalancesResult result = this.addressIndexer.GetAddressIndexerState(addressesArray);

                return this.Json(result);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>Returns every UTXO as of a given block height. This may take some time for large chains.</summary>
        /// <param name="atBlockHeight">Only process blocks up to this height for the purposes of constructing the UTXO set.</param>
        /// <returns>A result object containing the UTXOs.</returns>
        /// <response code="200">Returns the UTXO set.</response>
        /// <response code="400">Unexpected exception occurred</response>
        [Route(BlockStoreRouteEndPoint.GetUtxoSet)]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public IActionResult GetUtxoSet(int atBlockHeight)
        {
            try
            {
                ReconstructedCoinviewContext coinView = this.utxoIndexer.GetCoinviewAtHeight(atBlockHeight);

                var outputs = new List<UtxoModel>();

                foreach (OutPoint outPoint in coinView.UnspentOutputs)
                {
                    TxOut txOut = coinView.Transactions[outPoint.Hash].Outputs[outPoint.N];
                    var utxo = new UtxoModel() { TxId = outPoint.Hash, Index = outPoint.N, ScriptPubKey = txOut.ScriptPubKey, Value = txOut.Value };

                    outputs.Add(utxo);
                }

                return this.Json(outputs);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}