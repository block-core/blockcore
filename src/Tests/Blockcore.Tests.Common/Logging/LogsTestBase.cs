﻿using System;
using System.Collections.Generic;
using Blockcore.AsyncWork;
using Blockcore.Configuration.Logging;
using Blockcore.Networks;
using Blockcore.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;

namespace Blockcore.Tests.Common.Logging
{
    public class LogsTestBase : TestBase
    {
        /// <remarks>
        /// This class is not able to work concurrently because logs is a static class.
        /// The logs class needs to be refactored first before tests can run in parallel.
        /// </remarks>
        public LogsTestBase(Network network = null) : base(network ?? KnownNetworks.Main)
        {
            this.FullNodeLogger = new Mock<ILogger>();
            this.RPCLogger = new Mock<ILogger>();
            this.Logger = new Mock<ILogger>();
            this.LoggerFactory = new Mock<ILoggerFactory>();

            Initialise();
        }

        private void Initialise()
        {
            this.LoggerFactory.Setup(l => l.CreateLogger(It.IsAny<string>()))
               .Returns(this.Logger.Object);
            this.LoggerFactory.Setup(l => l.CreateLogger(typeof(FullNode).FullName))
               .Returns(this.FullNodeLogger.Object)
               .Verifiable();
            /*
            // TODO: Re-factor by moving to Blockcore.Features.RPC.Tests or Blockcore.IntegrationTests
            this.mockLoggerFactory.Setup(l => l.CreateLogger(typeof(RPCFeature).FullName))
                .Returns(this.rpcLogger.Object)
                 .Verifiable();
            */
        }

        public Mock<ILoggerFactory> LoggerFactory { get; }

        public Mock<ILogger> FullNodeLogger { get; }

        public Mock<ILogger> RPCLogger { get; }

        public Mock<ILogger> Logger { get; }

        protected void AssertLog<T>(Mock<ILogger> logger, LogLevel logLevel, string exceptionMessage, string message) where T : Exception
        {
            logger
                .Setup(f => f.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(new InvocationAction(invocation =>
                {
                    if ((LogLevel)invocation.Arguments[0] == logLevel)
                    {
                        invocation.Arguments[2].ToString().Should().EndWith(message);
                        ((T)invocation.Arguments[3]).Message.Should().Be(exceptionMessage);
                    }
                }));
        }

        protected void AssertLog<T>(Mock<ILogger<FullNode>> logger, LogLevel logLevel, string exceptionMessage, string message) where T : Exception
        {
            logger
                .Setup(f => f.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(new InvocationAction(invocation =>
                {
                    if ((LogLevel)invocation.Arguments[0] == logLevel)
                    {
                        invocation.Arguments[2].ToString().Should().EndWith(message);
                        ((T)invocation.Arguments[3]).Message.Should().Be(exceptionMessage);
                    }
                }));
        }

        protected void AssertLog(Mock<ILogger> logger, LogLevel logLevel, string message)
        {
            logger
                .Setup(f => f.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(new InvocationAction(invocation =>
                {
                    if ((LogLevel)invocation.Arguments[0] == logLevel)
                    {
                        invocation.Arguments[2].ToString().Should().EndWith(message);
                    }
                }));
        }

        /* TODO: Re-factor
        protected void AssertLog(Mock<ILogger<RPCMiddleware>> logger, LogLevel logLevel, string message)
        {
            logger.Verify(f => f.Log<Object>(logLevel,
                It.IsAny<EventId>(),
                It.Is<object>(l => ((FormattedLogValues)l)[0].Value.ToString().EndsWith(message)),
                null,
                It.IsAny<Func<object, Exception, string>>()));
        }
        */

        protected void AssertLog(Mock<ILogger<FullNode>> logger, LogLevel logLevel, string message)
        {
            logger.Verify(f => f.Log<Object>(logLevel,
                It.IsAny<EventId>(),
                It.Is<object>(l => ((IReadOnlyList<KeyValuePair<string, object>>)l)[0].Value.ToString().EndsWith(message)),
                null,
                It.IsAny<Func<object, Exception, string>>()));
        }

        protected IAsyncProvider CreateAsyncProvider()
        {
            var loggerFactory = new ExtendedLoggerFactory();
            var signals = new Signals.Signals(loggerFactory, null);
            var nodeLifetime = new NodeLifetime();
            var asyncProvider = new AsyncProvider(loggerFactory, signals, nodeLifetime);

            return asyncProvider;
        }
    }
}