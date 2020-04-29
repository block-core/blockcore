using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Base;
using Blockcore.Connection;
using Blockcore.Interfaces;
using Blockcore.Utilities;
using NBitcoin;

namespace Dashboard.Data
{
    public class IndexService
    {
        public IndexService(Network network, IFullNode fullNode, IConnectionManager connectionManager, ChainIndexer chainIndexer, IInitialBlockDownloadState initialBlockDownloadState, IDateTimeProvider dateTimeProvider)
        {
            this.Network = network;
            this.FullNode = fullNode;
            this.ConnectionManager = connectionManager;
            this.ChainIndexer = chainIndexer;
            this.InitialBlockDownloadState = initialBlockDownloadState;
            this.DateTimeProvider = dateTimeProvider;
        }

        public Network Network { get; }
        public IFullNode FullNode { get; }
        public IConnectionManager ConnectionManager { get; }
        public ChainIndexer ChainIndexer { get; }
        public IInitialBlockDownloadState InitialBlockDownloadState { get; }
        public IDateTimeProvider DateTimeProvider { get; }
    }
}