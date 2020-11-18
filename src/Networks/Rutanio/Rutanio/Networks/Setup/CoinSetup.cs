using System;
using System.Collections.Generic;
using Blockcore.Consensus.Checkpoints;
using NBitcoin;

namespace Rutanio.Networks.Setup
{
    internal class CoinSetup
    {
        internal string FileNamePrefix;
        internal string ConfigFileName;
        internal string Magic;
        internal int CoinType;
        internal decimal PremineReward;
        internal decimal PoWBlockReward;
        internal decimal PoSBlockReward;
        internal int LastPowBlock;
        internal string GenesisText;
        internal TimeSpan TargetSpacing;
        internal uint ProofOfStakeTimestampMask;
        internal int PoSVersion;
    }

    internal class NetworkSetup
    {
        internal string Name;
        internal string RootFolderName;
        internal string CoinTicker;
        internal int DefaultPort;
        internal int DefaultRPCPort;
        internal int DefaultAPIPort;
        internal int DefaultSignalRPort;
        internal int PubKeyAddress;
        internal int ScriptAddress;
        internal int SecretAddress;
        internal uint GenesisTime;
        internal uint GenesisNonce;
        internal uint GenesisBits;
        internal int GenesisVersion;
        internal Money GenesisReward;
        internal string HashGenesisBlock;
        internal string HashMerkleRoot;
        internal string[] DNS;
        internal string[] Nodes;
        internal Dictionary<int, CheckpointInfo> Checkpoints;
    }
}