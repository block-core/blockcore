﻿using System;
using System.IO;
using System.Threading;
using Blockcore.AsyncWork;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Networks;
using Blockcore.P2P.Peer;
using Blockcore.Signals;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Dns.Tests
{
    /// <summary>
    /// Tests for the <see cref="DnsFeature"/> class.
    /// </summary>
    public class GivenADnsFeature : TestBase
    {
        private class TestContext
        {
            public Mock<IDnsServer> dnsServer;
            public Mock<IWhitelistManager> whitelistManager;
            public Mock<ILoggerFactory> loggerFactory;
            public Mock<INodeLifetime> nodeLifetime;
            public DnsSettings dnsSettings;
            public NodeSettings nodeSettings;
            public DataFolder dataFolder;
            public IAsyncProvider asyncProvider;
            public ISignals signals;
            public Mock<IConnectionManager> connectionManager;
            public UnreliablePeerBehavior unreliablePeerBehavior;

            public TestContext(Network network)
            {
                this.dnsServer = new Mock<IDnsServer>();
                this.whitelistManager = new Mock<IWhitelistManager>();

                var logger = new Mock<ILogger>(MockBehavior.Loose);
                this.loggerFactory = new Mock<ILoggerFactory>();
                this.loggerFactory.Setup<ILogger>(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

                this.nodeLifetime = new Mock<INodeLifetime>();
                this.nodeSettings = new NodeSettings(network, args: new string[] { $"-datadir={Directory.GetCurrentDirectory()}" });
                this.dnsSettings = new DnsSettings(this.nodeSettings);
                this.dataFolder = CreateDataFolder(this);
                this.asyncProvider = new Mock<IAsyncProvider>().Object;
                this.connectionManager = this.BuildConnectionManager();
                this.unreliablePeerBehavior = this.BuildUnreliablePeerBehavior();
                this.signals = new Signals.Signals(this.loggerFactory.Object, null);
            }

            private Mock<IConnectionManager> BuildConnectionManager()
            {
                NetworkPeerConnectionParameters networkPeerParameters = new NetworkPeerConnectionParameters();
                Mock<IConnectionManager> connectionManager = new Mock<IConnectionManager>();
                connectionManager.SetupGet(np => np.Parameters).Returns(networkPeerParameters);
                connectionManager.SetupGet(np => np.ConnectedPeers).Returns(new NetworkPeerCollection());
                return connectionManager;
            }

            private UnreliablePeerBehavior BuildUnreliablePeerBehavior()
            {
                IChainState chainState = new Mock<IChainState>().Object;
                IPeerBanning peerBanning = new Mock<IPeerBanning>().Object;
                Checkpoints checkpoints = new Checkpoints();
                return new UnreliablePeerBehavior(KnownNetworks.StratisMain, chainState, this.loggerFactory.Object, peerBanning, this.nodeSettings, checkpoints);
            }
        }

        private readonly TestContext defaultConstructorParameters;

        public GivenADnsFeature() : base(KnownNetworks.Main)
        {
            this.defaultConstructorParameters = new TestContext(this.Network);
        }

        /// <summary>
        /// Builds the default DNS feature using default constructor parameters.
        /// </summary>
        /// <returns>DnsFeature instance.</returns>
        private DnsFeature BuildDefaultDnsFeature()
        {
            return new DnsFeature(
                this.defaultConstructorParameters.dnsServer?.Object,
                this.defaultConstructorParameters.whitelistManager?.Object,
                this.defaultConstructorParameters.loggerFactory?.Object,
                this.defaultConstructorParameters.nodeLifetime?.Object,
                this.defaultConstructorParameters.dnsSettings,
                this.defaultConstructorParameters.nodeSettings,
                this.defaultConstructorParameters.dataFolder,
                this.defaultConstructorParameters.asyncProvider,
                this.defaultConstructorParameters.connectionManager?.Object,
                this.defaultConstructorParameters.unreliablePeerBehavior
            );
        }

        [Fact]
        public void WhenConstructorCalled_AndDnsServerIsNull_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange.
            this.defaultConstructorParameters.dnsServer = null;
            Action a = () => this.BuildDefaultDnsFeature();

            // Act and Assert.
            a.Should().Throw<ArgumentNullException>().Which.Message.Should().Contain("dnsServer");
        }

        [Fact]
        public void WhenConstructorCalled_AndWhiteListManagerIsNull_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange.
            this.defaultConstructorParameters.whitelistManager = null;
            Action a = () => this.BuildDefaultDnsFeature();

            // Act and Assert.
            a.Should().Throw<ArgumentNullException>().Which.Message.Should().Contain("whitelistManager");
        }

        [Fact]
        public void WhenConstructorCalled_AndLoggerFactoryIsNull_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange.
            this.defaultConstructorParameters.loggerFactory = null;
            Action a = () => this.BuildDefaultDnsFeature();

            // Act and Assert.
            a.Should().Throw<ArgumentNullException>().Which.Message.Should().Contain("loggerFactory");
        }

        [Fact]
        public void WhenConstructorCalled_AndNodeLifetimeIsNull_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange.
            this.defaultConstructorParameters.nodeLifetime = null;
            Action a = () => this.BuildDefaultDnsFeature();

            // Act and Assert.
            a.Should().Throw<ArgumentNullException>().Which.Message.Should().Contain("nodeLifetime");
        }

        [Fact]
        public void WhenConstructorCalled_AndNodeSettingsIsNull_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange.
            this.defaultConstructorParameters.nodeSettings = null;
            Action a = () => this.BuildDefaultDnsFeature();

            // Act and Assert.
            a.Should().Throw<ArgumentNullException>().Which.Message.Should().Contain("nodeSettings");
        }

        [Fact]
        public void WhenConstructorCalled_AndDataFolderIsNull_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange.
            this.defaultConstructorParameters.dataFolder = null;
            Action a = () => this.BuildDefaultDnsFeature();

            // Act and Assert.
            a.Should().Throw<ArgumentNullException>().Which.Message.Should().Contain("dataFolder");
        }

        [Fact]
        public void WhenConstructorCalled_AndAllParametersValid_ThenTypeCreated()
        {
            // Arrange.
            var feature = this.BuildDefaultDnsFeature();

            // Assert.
            feature.Should().NotBeNull();
        }

        [Fact]
        public void WhenDnsFeatureInitialized_ThenDnsServerSuccessfullyStarts()
        {
            // Arrange.
            var waitObject = new ManualResetEventSlim(false);
            Action<int, CancellationToken> action = (port, token) =>
            {
                waitObject.Set();
                throw new OperationCanceledException();
            };
            this.defaultConstructorParameters.dnsServer.Setup(s => s.ListenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Callback(action);

            // Act.
            var feature = this.BuildDefaultDnsFeature();
            feature.InitializeAsync().GetAwaiter().GetResult();
            bool waited = waitObject.Wait(5000);

            // Assert.
            feature.Should().NotBeNull();
            waited.Should().BeTrue();
            this.defaultConstructorParameters.dnsServer.Verify(s => s.ListenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void WhenDnsFeatureStopped_ThenDnsServerSuccessfullyStops()
        {
            // Arrange.
            Action<int, CancellationToken> action = (port, token) =>
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(50);
                }
            };
            this.defaultConstructorParameters.dnsServer.Setup(s => s.ListenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Callback(action);

            var source = new CancellationTokenSource();
            this.defaultConstructorParameters.nodeLifetime.Setup(n => n.StopApplication()).Callback(() => source.Cancel());
            this.defaultConstructorParameters.nodeLifetime.Setup(n => n.ApplicationStopping).Returns(source.Token);

            // Act.
            var feature = this.BuildDefaultDnsFeature();
            feature.InitializeAsync().GetAwaiter().GetResult();
            this.defaultConstructorParameters.nodeLifetime.Object.StopApplication();
            bool waited = source.Token.WaitHandle.WaitOne(5000);

            // Assert.
            feature.Should().NotBeNull();
            waited.Should().BeTrue();
            this.defaultConstructorParameters.dnsServer.Verify(s => s.ListenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void WhenDnsServerFailsToStart_ThenDnsFeatureRetries()
        {
            // Arrange.
            Action<int, CancellationToken> action = (port, token) =>
            {
                throw new ArgumentException("Bad port");
            };
            this.defaultConstructorParameters.dnsServer.Setup(s => s.ListenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Callback(action);

            var source = new CancellationTokenSource(3000);
            this.defaultConstructorParameters.nodeLifetime.Setup(n => n.StopApplication()).Callback(() => source.Cancel());
            this.defaultConstructorParameters.nodeLifetime.Setup(n => n.ApplicationStopping).Returns(source.Token);

            var logger = new Mock<ILogger>();
            bool serverError = false;
            logger
                .Setup(f => f.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(new InvocationAction(invocation =>
                {
                    if (!serverError && (LogLevel)invocation.Arguments[0] == LogLevel.Error)
                    {
                        // Not yet set, check trace message
                        serverError = invocation.Arguments[2].ToString().StartsWith("Failed whilst running the DNS server");
                    }
                }));
            this.defaultConstructorParameters.loggerFactory.Setup<ILogger>(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

            // Act.
            var feature = this.BuildDefaultDnsFeature();
            feature.InitializeAsync().GetAwaiter().GetResult();
            bool waited = source.Token.WaitHandle.WaitOne(5000);

            // Assert.
            feature.Should().NotBeNull();
            waited.Should().BeTrue();
            this.defaultConstructorParameters.dnsServer.Verify(s => s.ListenAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
            serverError.Should().BeTrue();
        }

        [Fact]
        public void WhenInitialize_ThenRefreshLoopIsStarted()
        {
            // Arrange.
            this.defaultConstructorParameters.whitelistManager.Setup(w => w.RefreshWhitelist()).Verifiable("the RefreshWhitelist method should be called on the WhitelistManager");

            var source = new CancellationTokenSource(3000);
            this.defaultConstructorParameters.nodeLifetime.Setup(n => n.StopApplication()).Callback(() => source.Cancel());
            this.defaultConstructorParameters.nodeLifetime.Setup(n => n.ApplicationStopping).Returns(source.Token);

            this.defaultConstructorParameters.asyncProvider = new AsyncProvider(this.defaultConstructorParameters.loggerFactory.Object, this.defaultConstructorParameters.signals, this.defaultConstructorParameters.nodeLifetime.Object);

            using (var feature = this.BuildDefaultDnsFeature())
            {
                // Act.
                feature.InitializeAsync().GetAwaiter().GetResult();
                bool waited = source.Token.WaitHandle.WaitOne(5000);

                // Assert.
                feature.Should().NotBeNull();
                waited.Should().BeTrue();
                this.defaultConstructorParameters.whitelistManager.Verify();
            }
        }
    }
}