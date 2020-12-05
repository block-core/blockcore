using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Base
{
    /// <summary>
    /// Provides IBD (Initial Block Download) state.
    /// </summary>
    /// <seealso cref="IInitialBlockDownloadState" />
    public class InitialBlockDownloadState : IInitialBlockDownloadState
    {
        /// <summary>A provider of the date and time.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Provider of block header hash checkpoints.</summary>
        private readonly ICheckpoints checkpoints;

        /// <summary>Information about node's chain.</summary>
        private readonly IChainState chainState;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>User defined consensus settings.</summary>
        private readonly ConsensusSettings consensusSettings;

        private int lastCheckpointHeight;
        private uint256 minimumChainWork;

        public InitialBlockDownloadState(IChainState chainState, Network network, ConsensusSettings consensusSettings, ICheckpoints checkpoints, ILoggerFactory loggerFactory, IDateTimeProvider dateTimeProvider)
        {
            Guard.NotNull(chainState, nameof(chainState));

            this.network = network;
            this.consensusSettings = consensusSettings;
            this.chainState = chainState;
            this.checkpoints = checkpoints;
            this.dateTimeProvider = dateTimeProvider;

            this.lastCheckpointHeight = this.checkpoints.GetLastCheckpointHeight();
            this.minimumChainWork = this.network.Consensus.MinimumChainWork ?? uint256.Zero;

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <inheritdoc />
        public bool IsInitialBlockDownload()
        {
            if (this.lastCheckpointHeight > this.chainState.ConsensusTip.Height)
                return true;

            if (this.chainState.ConsensusTip.Header.BlockTime < (this.dateTimeProvider.GetUtcNow().AddSeconds(-this.consensusSettings.MaxTipAge)))
                return true;

            if (this.chainState.ConsensusTip.ChainWork < this.minimumChainWork)
                return true;

            return false;
        }
    }
}