using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Consensus.Persistence.LevelDb;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.IntegrationTests
{
    public class NodeContext : IDisposable
    {
        /// <summary>Factory for creating loggers.</summary>
        protected readonly ILoggerFactory loggerFactory;

        private readonly List<IDisposable> cleanList;

        public NodeContext(object caller, string name, Network network, bool clean)
        {
            network = network ?? KnownNetworks.RegTest;
            this.loggerFactory = new LoggerFactory();
            this.Network = network;
            this.FolderName = TestBase.CreateTestDir(caller, name);
            var dateTimeProvider = new DateTimeProvider();
            var serializer = new DataStoreSerializer(this.Network.Consensus.ConsensusFactory);
            //this.Coindb = new DBreezeCoindb(network, this.FolderName, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), serializer);
            //this.Coindb = new FasterCoindb(network, this.FolderName, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), serializer);
            this.Coindb = new LevelDbCoindb(network, this.FolderName, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), serializer);
            this.Coindb.Initialize();
            this.cleanList = new List<IDisposable> { (IDisposable)this.Coindb };
        }

        public Network Network { get; }

        private ChainBuilder chainBuilder;

        public ChainBuilder ChainBuilder
        {
            get
            {
                return this.chainBuilder = this.chainBuilder ?? new ChainBuilder(this.Network);
            }
        }

        public ICoindb Coindb { get; private set; }

        public string FolderName { get; }

        public static NodeContext Create(object caller, [CallerMemberName]string name = null, Network network = null, bool clean = true)
        {
            return new NodeContext(caller, name, network, clean);
        }

        public void Dispose()
        {
            foreach (IDisposable item in this.cleanList)
                item.Dispose();
        }

        public void ReloadPersistentCoinView()
        {
            ((IDisposable)this.Coindb).Dispose();
            this.cleanList.Remove((IDisposable)this.Coindb);
            var dateTimeProvider = new DateTimeProvider();
            var serializer = new DataStoreSerializer(this.Network.Consensus.ConsensusFactory);
            //this.Coindb = new DBreezeCoindb(this.Network, this.FolderName, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), serializer);
            //this.Coindb = new FasterCoindb(this.Network, this.FolderName, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), serializer);
            this.Coindb = new LevelDbCoindb(this.Network, this.FolderName, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), serializer);

            this.Coindb.Initialize();
            this.cleanList.Add((IDisposable)this.Coindb);
        }
    }
}