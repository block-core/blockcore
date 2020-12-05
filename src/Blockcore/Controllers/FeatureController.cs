using System;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Connection;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Networks;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;

namespace Blockcore.Controllers
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ActionDescription : Attribute
    {
        public string Description { get; private set; }

        public ActionDescription(string description)
        {
            this.Description = description;
        }
    }

    public abstract class FeatureController : Controller
    {
        protected IFullNode FullNode { get; set; }

        protected NodeSettings Settings { get; set; }

        protected Network Network { get; set; }

        protected ChainIndexer ChainIndexer { get; set; }

        protected IChainState ChainState { get; set; }

        protected IConnectionManager ConnectionManager { get; set; }

        protected IConsensusManager ConsensusManager { get; private set; }

        public FeatureController(
            IFullNode fullNode = null,
            Network network = null,
            NodeSettings nodeSettings = null,
            ChainIndexer chainIndexer = null,
            IChainState chainState = null,
            IConnectionManager connectionManager = null,
            IConsensusManager consensusManager = null)
        {
            this.FullNode = fullNode;
            this.Settings = nodeSettings;
            this.Network = network;
            this.ChainIndexer = chainIndexer;
            this.ChainState = chainState;
            this.ConnectionManager = connectionManager;
            this.ConsensusManager = consensusManager;
        }
    }
}