using System;
using System.Collections.Generic;
using System.Net;
using Blockcore.Base;
using Blockcore.Base.Deployments;
using Blockcore.Base.Deployments.Models;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Controllers;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonErrors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Features.Consensus
{
    /// <summary>
    /// A <see cref="FeatureController"/> that provides API and RPC methods from the consensus loop.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class ConsensusController : FeatureController
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public ConsensusController(
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
        /// Get the threshold states of softforks currently being deployed.
        /// Allowable states are: Defined, Started, LockedIn, Failed, Active.
        /// </summary>
        /// <returns>A <see cref="JsonResult"/> object derived from a list of
        /// <see cref="ThresholdStateModel"/> objects - one per deployment.
        /// Returns an <see cref="ErrorResult"/> if the method fails.</returns>
        [Route("deploymentflags")]
        [HttpGet]
        public IActionResult DeploymentFlags()
        {
            try
            {
                ConsensusRuleEngine ruleEngine = this.ConsensusManager.ConsensusRules as ConsensusRuleEngine;

                // Ensure threshold conditions cached.
                ThresholdState[] thresholdStates = ruleEngine.NodeDeployments.BIP9.GetStates(this.ChainState.ConsensusTip.Previous);

                List<ThresholdStateModel> metrics = ruleEngine.NodeDeployments.BIP9.GetThresholdStateMetrics(this.ChainState.ConsensusTip.Previous, thresholdStates);

                return this.Json(metrics);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets the hash of the block at the consensus tip.
        /// </summary>
        /// <returns>Json formatted <see cref="uint256"/> hash of the block at the consensus tip. Returns <see cref="IActionResult"/> formatted error if fails.</returns>
        /// <remarks>This is an API implementation of an RPC call.</remarks>
        [Route("getbestblockhash")]
        [HttpGet]
        public IActionResult GetBestBlockHash()
        {
            try
            {
                return this.Json(this.ChainState.ConsensusTip?.HashBlock);
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }

        /// <summary>
        /// Gets the hash of the block at a given height.
        /// </summary>
        /// <param name="height">The height of the block to get the hash for.</param>
        /// <returns>Json formatted <see cref="uint256"/> hash of the block at the given height. <c>Null</c> if block not found. Returns <see cref="IActionResult"/> formatted error if fails.</returns>
        /// <remarks>This is an API implementation of an RPC call.</remarks>
        [Route("getblockhash")]
        [HttpGet]
        public IActionResult GetBlockHash([FromQuery] int height)
        {
            try
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

                return this.Json(hash);
            }
            catch (Exception e)
            {
                this.logger.LogTrace("(-)[EXCEPTION]");
                this.logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}
