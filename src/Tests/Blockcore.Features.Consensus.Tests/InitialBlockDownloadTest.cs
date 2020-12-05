using System;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Consensus.Tests
{
    public class InitialBlockDownloadTest
    {
        private readonly ConsensusSettings consensusSettings;
        private readonly Checkpoints checkpoints;
        private readonly ChainState chainState;
        private readonly Network network;
        private readonly Mock<ILoggerFactory> loggerFactory;

        public InitialBlockDownloadTest()
        {
            this.network = KnownNetworks.Main;
            this.consensusSettings = new ConsensusSettings(new NodeSettings(this.network));
            this.checkpoints = new Checkpoints(this.network, this.consensusSettings);
            this.chainState = new ChainState();
            this.loggerFactory = new Mock<ILoggerFactory>();
            this.loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        }

        [Fact]
        public void InIBDIfBehindCheckpoint()
        {
            BlockHeader blockHeader = this.network.Consensus.ConsensusFactory.CreateBlockHeader();
            this.chainState.ConsensusTip = new ChainedHeader(blockHeader, blockHeader.GetHash(), 1000);
            var blockDownloadState = new InitialBlockDownloadState(this.chainState, this.network, this.consensusSettings, this.checkpoints, this.loggerFactory.Object, DateTimeProvider.Default);
            Assert.True(blockDownloadState.IsInitialBlockDownload());
        }

        [Fact]
        public void InIBDIfChainWorkIsLessThanMinimum()
        {
            BlockHeader blockHeader = this.network.Consensus.ConsensusFactory.CreateBlockHeader();
            this.chainState.ConsensusTip = new ChainedHeader(blockHeader, blockHeader.GetHash(), this.checkpoints.GetLastCheckpointHeight() + 1);
            var blockDownloadState = new InitialBlockDownloadState(this.chainState, this.network, this.consensusSettings, this.checkpoints, this.loggerFactory.Object, DateTimeProvider.Default);
            Assert.True(blockDownloadState.IsInitialBlockDownload());
        }

        [Fact]
        public void InIBDIfTipIsOlderThanMaxAge()
        {
            BlockHeader blockHeader = this.network.Consensus.ConsensusFactory.CreateBlockHeader();

            // Enough work to get us past the chain work check.
            blockHeader.Bits = new Target(new uint256(uint.MaxValue));

            // Block has a time sufficiently in the past that it can't be the tip.
            blockHeader.Time = ((uint) DateTimeOffset.Now.ToUnixTimeSeconds()) - (uint) this.network.MaxTipAge - 1;

            this.chainState.ConsensusTip = new ChainedHeader(blockHeader, blockHeader.GetHash(), this.checkpoints.GetLastCheckpointHeight() + 1);
            var blockDownloadState = new InitialBlockDownloadState(this.chainState, this.network, this.consensusSettings, this.checkpoints, this.loggerFactory.Object, DateTimeProvider.Default);
            Assert.True(blockDownloadState.IsInitialBlockDownload());
        }
    }
}