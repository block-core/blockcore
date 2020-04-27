using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore;
using Blockcore.Base;
using Blockcore.Connection;
using Blockcore.Interfaces;
using NBitcoin;

namespace Dashboard.Data
{
    public class IndexService
    {
        public IndexService(Network network, IFullNode fullNode, IConnectionManager connectionManager, ChainIndexer chainIndexer, IInitialBlockDownloadState initialBlockDownloadState)
        {
            this.Network = network;
            this.FullNode = fullNode;
            this.ConnectionManager = connectionManager;
            this.ChainIndexer = chainIndexer;
            this.InitialBlockDownloadState = initialBlockDownloadState;
        }

        public Network Network { get; }
        public IFullNode FullNode { get; }
        public IConnectionManager ConnectionManager { get; }
        public ChainIndexer ChainIndexer { get; }
        public IInitialBlockDownloadState InitialBlockDownloadState { get; }
    }
}