using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Base;
using Blockcore.Base.Deployments;
using Blockcore.Configuration;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.Rules;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Features.Consensus.Rules;
using Blockcore.Features.Consensus.Rules.CommonRules;
using Blockcore.Features.Consensus.Rules.UtxosetRules;
using Blockcore.Features.MemoryPool.Fee;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.MemoryPool.Rules;
using Blockcore.Features.Miner;
using Blockcore.Interfaces;
using Blockcore.Mining;
using Blockcore.Networks;
using Blockcore.Signals;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;

using Xunit;

namespace Blockcore.Features.MemoryPool.Tests
{
    /// <summary>
    /// Public interface for test chain context.
    /// </summary>
    public interface ITestChainContext
    {
        /// <summary>
        /// Memory pool validator interface;
        /// </summary>
        IMempoolValidator MempoolValidator { get; }

        /// <summary>
        /// The settings for the mempool.
        /// </summary>
        MempoolSettings MempoolSettings { get; }

        /// <summary>
        /// The chain.
        /// </summary>
        ChainIndexer ChainIndexer { get; }

        /// <summary>
        /// List of the source transactions in the test chain.
        /// </summary>
        List<Transaction> SrcTxs { get; }
    }

    /// <summary>
    /// Concrete instance of the test chain.
    /// </summary>
    internal class TestChainContext : ITestChainContext
    {
        /// <inheritdoc />
        public IMempoolValidator MempoolValidator { get; set; }

        /// <inheritdoc />
        public MempoolSettings MempoolSettings { get; set; }

        /// <inheritdoc />
        public ChainIndexer ChainIndexer { get; set; }

        /// <inheritdoc />
        public List<Transaction> SrcTxs { get; set; }
    }

    /// <summary>
    /// Factory for creating the test chain.
    /// Much of this logic was taken directly from the embedded TestContext class in MinerTest.cs in the integration tests.
    /// </summary>
    internal class TestChainFactory
    {
        public static async Task<ITestChainContext> CreatePosAsync(Network network, Script scriptPubKey, string dataDir, bool requireStandard = true)
        {
            var nodeSettings = new NodeSettings(network, args: new string[] { $"-datadir={dataDir}" });

            ILoggerFactory loggerFactory = nodeSettings.LoggerFactory;
            IDateTimeProvider dateTimeProvider = DateTimeProvider.Default;

            network.Consensus.Options = new ConsensusOptions();

            var consensusRulesContainer = new ConsensusRulesContainer();
            foreach (var ruleType in network.Consensus.ConsensusRules.HeaderValidationRules)
            {
                // Don't check PoW of a header in this test.
                if (ruleType == typeof(CheckDifficultyPowRule))
                    continue;

                consensusRulesContainer.HeaderValidationRules.Add(Activator.CreateInstance(ruleType) as HeaderValidationConsensusRule);
            }
            foreach (Type ruleType in network.Consensus.ConsensusRules.FullValidationRules)
                consensusRulesContainer.FullValidationRules.Add(Activator.CreateInstance(ruleType) as FullValidationConsensusRule);
            foreach (Type ruleType in network.Consensus.ConsensusRules.PartialValidationRules)
                consensusRulesContainer.PartialValidationRules.Add(Activator.CreateInstance(ruleType) as PartialValidationConsensusRule);

            var consensusSettings = new ConsensusSettings(nodeSettings);
            var chain = new ChainIndexer(network);
            var inMemoryCoinView = new InMemoryCoinView(new HashHeightPair(chain.Tip));

            var chainState = new ChainState();
            var deployments = new NodeDeployments(network, chain);

            var asyncProvider = new AsyncProvider(loggerFactory, new Mock<ISignals>().Object, new NodeLifetime());

            var stakeChain = new StakeChainStore(network, chain, null, loggerFactory);
            ConsensusRuleEngine consensusRules = new PosConsensusRuleEngine(network, loggerFactory, dateTimeProvider, chain, deployments, consensusSettings, new Checkpoints(),
                inMemoryCoinView, stakeChain, new StakeValidator(network, stakeChain, chain, inMemoryCoinView, loggerFactory), chainState, new InvalidBlockHashStore(dateTimeProvider),
                new NodeStats(dateTimeProvider, loggerFactory), new RewindDataIndexCache(dateTimeProvider, network, new FinalizedBlockInfoRepository(new HashHeightPair()), new Checkpoints()), asyncProvider, consensusRulesContainer).SetupRulesEngineParent();

            ConsensusManager consensus = ConsensusManagerHelper.CreateConsensusManager(network, dataDir, chainState, chainIndexer: chain, consensusRules: consensusRules, inMemoryCoinView: inMemoryCoinView);

            var genesis = new ChainedHeader(network.GetGenesis().Header, network.GenesisHash, 0);
            chainState.BlockStoreTip = genesis;
            await consensus.InitializeAsync(genesis).ConfigureAwait(false);

            var mempoolSettings = new MempoolSettings(nodeSettings) { RequireStandard = requireStandard };
            var blockPolicyEstimator = new BlockPolicyEstimator(mempoolSettings, loggerFactory, nodeSettings);
            var mempool = new TxMempool(dateTimeProvider, blockPolicyEstimator, loggerFactory, nodeSettings);
            var mempoolLock = new MempoolSchedulerLock();

            // The mempool rule constructors aren't parameterless, so we have to manually inject the dependencies for every rule
            var mempoolRules = new List<MempoolRule>
            {
                new CheckConflictsMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckCoinViewMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CreateMempoolEntryMempoolRule(network, mempool, mempoolSettings, chain, consensusRules, loggerFactory),
                new CheckSigOpsMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckFeeMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckRateLimitMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckAncestorsMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckReplacementMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckAllInputsMempoolRule(network, mempool, mempoolSettings, chain, consensusRules, deployments, loggerFactory),
                new CheckTxOutDustRule(network, mempool, mempoolSettings, chain, loggerFactory),
            };

            // We also have to check that the manually instantiated rules match the ones in the network, or the test isn't valid
            for (int i = 0; i < network.Consensus.MempoolRules.Count; i++)
            {
                if (network.Consensus.MempoolRules[i] != mempoolRules[i].GetType())
                {
                    throw new Exception("Mempool rule type mismatch");
                }
            }

            var mempoolValidator = new MempoolValidator(mempool, mempoolLock, dateTimeProvider, mempoolSettings, chain, inMemoryCoinView, loggerFactory, nodeSettings, consensusRules, mempoolRules, deployments);

            var blocks = new List<Block>();
            var srcTxs = new List<Transaction>();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            for (int i = 0; i < 50; i++)
            {
                uint nonce = 0;
                Block block = network.CreateBlock();
                block.Header.HashPrevBlock = chain.Tip.HashBlock;
                block.Header.Bits = block.Header.GetWorkRequired(network, chain.Tip);
                block.Header.UpdateTime(now, network, chain.Tip);

                Transaction coinbase = network.CreateTransaction();
                coinbase.AddInput(TxIn.CreateCoinbase(chain.Height + 1));
                coinbase.AddOutput(new TxOut(network.Consensus.ProofOfWorkReward, scriptPubKey));
                block.AddTransaction(coinbase);
                block.UpdateMerkleRoot();
                while (!block.CheckProofOfWork())
                    block.Header.Nonce = ++nonce;
                block.Header.PrecomputeHash();
                blocks.Add(block);
                chain.SetTip(block.Header);
                srcTxs.Add(block.Transactions[0]);

                inMemoryCoinView.SaveChanges(new List<UnspentOutput>() { new UnspentOutput(new OutPoint(block.Transactions[0], 0), new Coins((uint)(i + 1), block.Transactions[0].Outputs.First(), block.Transactions[0].IsCoinBase)) }, new HashHeightPair(chain.Tip.Previous), new HashHeightPair(chain.Tip));
            }

            return new TestChainContext { MempoolValidator = mempoolValidator, MempoolSettings = mempoolSettings, ChainIndexer = chain, SrcTxs = srcTxs };
        }

        /// <summary>
        /// Creates the test chain with some default blocks and txs.
        /// </summary>
        /// <param name="network">Network to create the chain on.</param>
        /// <param name="scriptPubKey">Public key to create blocks/txs with.</param>
        /// <param name="requireStandard">By default testnet and regtest networks do not require transactions to be standard. This changes that default.</param>
        /// <returns>Context object representing the test chain.</returns>
        public static async Task<ITestChainContext> CreateAsync(Network network, Script scriptPubKey, string dataDir, bool requireStandard = true)
        {
            var nodeSettings = new NodeSettings(network, args: new string[] { $"-datadir={dataDir}" });

            ILoggerFactory loggerFactory = nodeSettings.LoggerFactory;
            IDateTimeProvider dateTimeProvider = DateTimeProvider.Default;

            network.Consensus.Options = new ConsensusOptions();

            var consensusRulesContainer = new ConsensusRulesContainer();
            foreach (Type ruleType in network.Consensus.ConsensusRules.HeaderValidationRules)
            {
                // Don't check PoW of a header in this test.
                if (ruleType == typeof(CheckDifficultyPowRule))
                    continue;

                consensusRulesContainer.HeaderValidationRules.Add(Activator.CreateInstance(ruleType) as HeaderValidationConsensusRule);
            }
            foreach (Type ruleType in network.Consensus.ConsensusRules.PartialValidationRules)
                consensusRulesContainer.PartialValidationRules.Add(Activator.CreateInstance(ruleType) as PartialValidationConsensusRule);
            foreach (var ruleType in network.Consensus.ConsensusRules.FullValidationRules)
            {
                FullValidationConsensusRule rule = null;
                if (ruleType == typeof(FlushUtxosetRule))
                    rule = new FlushUtxosetRule(new Mock<IInitialBlockDownloadState>().Object, new Mock<IChainRepository>().Object, new Mock<ChainIndexer>().Object, new Mock<INodeLifetime>().Object, new Mock<IChainState>().Object);
                else
                    rule = Activator.CreateInstance(ruleType) as FullValidationConsensusRule;

                consensusRulesContainer.FullValidationRules.Add(rule);
            }

            var consensusSettings = new ConsensusSettings(nodeSettings);
            var chain = new ChainIndexer(network);
            var inMemoryCoinView = new InMemoryCoinView(new HashHeightPair(chain.Tip));

            var asyncProvider = new AsyncProvider(loggerFactory, new Mock<ISignals>().Object, new NodeLifetime());

            var chainState = new ChainState();
            var deployments = new NodeDeployments(network, chain);

            ConsensusRuleEngine consensusRules = new PowConsensusRuleEngine(network, loggerFactory, dateTimeProvider, chain, deployments, consensusSettings, new Checkpoints(),
                inMemoryCoinView, chainState, new InvalidBlockHashStore(dateTimeProvider), new NodeStats(dateTimeProvider, loggerFactory), asyncProvider, consensusRulesContainer).SetupRulesEngineParent();

            ConsensusManager consensus = ConsensusManagerHelper.CreateConsensusManager(network, dataDir, chainState, chainIndexer: chain, consensusRules: consensusRules, inMemoryCoinView: inMemoryCoinView);

            var genesis = new ChainedHeader(network.GetGenesis().Header, network.GenesisHash, 0);
            chainState.BlockStoreTip = genesis;
            await consensus.InitializeAsync(genesis).ConfigureAwait(false);

            var blockPolicyEstimator = new BlockPolicyEstimator(new MempoolSettings(nodeSettings), loggerFactory, nodeSettings);
            var mempool = new TxMempool(dateTimeProvider, blockPolicyEstimator, loggerFactory, nodeSettings);
            var mempoolLock = new MempoolSchedulerLock();

            var minerSettings = new MinerSettings(nodeSettings);

            // Simple block creation, nothing special yet:
            var blockDefinition = new PowBlockDefinition(consensus, dateTimeProvider, loggerFactory, mempool, mempoolLock, minerSettings, network, consensusRules, deployments);
            BlockTemplate newBlock = blockDefinition.Build(chain.Tip, scriptPubKey);

            await consensus.BlockMinedAsync(newBlock.Block);

            List<BlockInfo> blockinfo = CreateBlockInfoList();

            // We can't make transactions until we have inputs therefore, load 100 blocks.

            var srcTxs = new List<Transaction>();
            for (int i = 0; i < blockinfo.Count; ++i)
            {
                Block currentBlock = Block.Load(newBlock.Block.ToBytes(network.Consensus.ConsensusFactory), network.Consensus.ConsensusFactory);
                currentBlock.Header.HashPrevBlock = chain.Tip.HashBlock;
                currentBlock.Header.Version = 1;
                currentBlock.Header.Time = Utils.DateTimeToUnixTime(chain.Tip.GetMedianTimePast()) + 1;

                Transaction txCoinbase = network.CreateTransaction(currentBlock.Transactions[0].ToBytes());
                txCoinbase.Inputs.Clear();
                txCoinbase.Version = 1;
                txCoinbase.AddInput(new TxIn(new Script(new[] { Op.GetPushOp(blockinfo[i].extraNonce), Op.GetPushOp(chain.Height) })));
                // Ignore the (optional) segwit commitment added by CreateNewBlock (as the hardcoded nonces don't account for this)
                txCoinbase.AddOutput(new TxOut(Money.Zero, new Script()));
                currentBlock.Transactions[0] = txCoinbase;

                currentBlock.UpdateMerkleRoot();
                currentBlock.Header.Nonce = blockinfo[i].nonce;

                chain.SetTip(currentBlock.Header);
                srcTxs.Add(currentBlock.Transactions[0]);

                inMemoryCoinView.SaveChanges(new List<UnspentOutput>() { new UnspentOutput(new OutPoint(currentBlock.Transactions[0], 0), new Coins((uint)(i + 1), currentBlock.Transactions[0].Outputs.First(), currentBlock.Transactions[0].IsCoinBase)) }, new HashHeightPair(chain.Tip.Previous), new HashHeightPair(chain.Tip));
            }

            // Just to make sure we can still make simple blocks
            blockDefinition = new PowBlockDefinition(consensus, dateTimeProvider, loggerFactory, mempool, mempoolLock, minerSettings, network, consensusRules, deployments);
            blockDefinition.Build(chain.Tip, scriptPubKey);

            var mempoolSettings = new MempoolSettings(nodeSettings) { RequireStandard = requireStandard };

            // The mempool rule constructors aren't parameterless, so we have to manually inject the dependencies for every rule
            var mempoolRules = new List<MempoolRule>
            {
                new CheckConflictsMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckCoinViewMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CreateMempoolEntryMempoolRule(network, mempool, mempoolSettings, chain, consensusRules, loggerFactory),
                new CheckSigOpsMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckFeeMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckRateLimitMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckAncestorsMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckReplacementMempoolRule(network, mempool, mempoolSettings, chain, loggerFactory),
                new CheckAllInputsMempoolRule(network, mempool, mempoolSettings, chain, consensusRules, deployments, loggerFactory),
                new CheckTxOutDustRule(network, mempool, mempoolSettings, chain, loggerFactory),
            };

            // We also have to check that the manually instantiated rules match the ones in the network, or the test isn't valid
            for (int i = 0; i < network.Consensus.MempoolRules.Count; i++)
            {
                if (network.Consensus.MempoolRules[i] != mempoolRules[i].GetType())
                {
                    throw new Exception("Mempool rule type mismatch");
                }
            }

            Assert.Equal(network.Consensus.MempoolRules.Count, mempoolRules.Count);

            var mempoolValidator = new MempoolValidator(mempool, mempoolLock, dateTimeProvider, mempoolSettings, chain, inMemoryCoinView, loggerFactory, nodeSettings, consensusRules, mempoolRules, deployments);

            return new TestChainContext { MempoolValidator = mempoolValidator, MempoolSettings = mempoolSettings, ChainIndexer = chain, SrcTxs = srcTxs };
        }

        /// <summary>
        /// Info for a single block in the hard coded blocks that are loaded.
        /// </summary>
        private class BlockInfo
        {
            /// <summary>
            /// Extra nonce for the block.
            /// </summary>
            public int extraNonce;

            /// <summary>
            /// Nonce for the block.
            /// </summary>
            public uint nonce;
        }

        /// <summary>
        /// Source block information for hard coded blocks that are loaded.
        /// Pairs of extranonce, nonce fields.
        /// </summary>
        private static long[,] blockinfoarr =
        {
            {4, 0xa4a3e223}, {2, 0x15c32f9e}, {1, 0x0375b547}, {1, 0x7004a8a5},
            {2, 0xce440296}, {2, 0x52cfe198}, {1, 0x77a72cd0}, {2, 0xbb5d6f84},
            {2, 0x83f30c2c}, {1, 0x48a73d5b}, {1, 0xef7dcd01}, {2, 0x6809c6c4},
            {2, 0x0883ab3c}, {1, 0x087bbbe2}, {2, 0x2104a814}, {2, 0xdffb6daa},
            {1, 0xee8a0a08}, {2, 0xba4237c1}, {1, 0xa70349dc}, {1, 0x344722bb},
            {3, 0xd6294733}, {2, 0xec9f5c94}, {2, 0xca2fbc28}, {1, 0x6ba4f406},
            {2, 0x015d4532}, {1, 0x6e119b7c}, {2, 0x43e8f314}, {2, 0x27962f38},
            {2, 0xb571b51b}, {2, 0xb36bee23}, {2, 0xd17924a8}, {2, 0x6bc212d9},
            {1, 0x630d4948}, {2, 0x9a4c4ebb}, {2, 0x554be537}, {1, 0xd63ddfc7},
            {2, 0xa10acc11}, {1, 0x759a8363}, {2, 0xfb73090d}, {1, 0xe82c6a34},
            {1, 0xe33e92d7}, {3, 0x658ef5cb}, {2, 0xba32ff22}, {5, 0x0227a10c},
            {1, 0xa9a70155}, {5, 0xd096d809}, {1, 0x37176174}, {1, 0x830b8d0f},
            {1, 0xc6e3910e}, {2, 0x823f3ca8}, {1, 0x99850849}, {1, 0x7521fb81},
            {1, 0xaacaabab}, {1, 0xd645a2eb}, {5, 0x7aea1781}, {5, 0x9d6e4b78},
            {1, 0x4ce90fd8}, {1, 0xabdc832d}, {6, 0x4a34f32a}, {2, 0xf2524c1c},
            {2, 0x1bbeb08a}, {1, 0xad47f480}, {1, 0x9f026aeb}, {1, 0x15a95049},
            {2, 0xd1cb95b2}, {2, 0xf84bbda5}, {1, 0x0fa62cd1}, {1, 0xe05f9169},
            {1, 0x78d194a9}, {5, 0x3e38147b}, {5, 0x737ba0d4}, {1, 0x63378e10},
            {1, 0x6d5f91cf}, {2, 0x88612eb8}, {2, 0xe9639484}, {1, 0xb7fabc9d},
            {2, 0x19b01592}, {1, 0x5a90dd31}, {2, 0x5bd7e028}, {2, 0x94d00323},
            {1, 0xa9b9c01a}, {1, 0x3a40de61}, {1, 0x56e7eec7}, {5, 0x859f7ef6},
            {1, 0xfd8e5630}, {1, 0x2b0c9f7f}, {1, 0xba700e26}, {1, 0x7170a408},
            {1, 0x70de86a8}, {1, 0x74d64cd5}, {1, 0x49e738a1}, {2, 0x6910b602},
            {0, 0x643c565f}, {1, 0x54264b3f}, {2, 0x97ea6396}, {2, 0x55174459},
            {2, 0x03e8779a}, {1, 0x98f34d8f}, {1, 0xc07b2b07}, {1, 0xdfe29668},
            {1, 0x3141c7c1}, {1, 0xb3b595f4}, {1, 0x735abf08}, {5, 0x623bfbce},
            {2, 0xd351e722}, {1, 0xf4ca48c9}, {1, 0x5b19c670}, {1, 0xa164bf0e},
            {2, 0xbbbeb305}, {2, 0xfe1c810a}
        };

        /// <summary>
        /// Translates an array of extranonce, nonce pairs into list of block info.
        /// </summary>
        /// <returns>List of block info for hard coded blocks.</returns>
        private static List<BlockInfo> CreateBlockInfoList()
        {
            var blockInfoList = new List<BlockInfo>();
            List<long> lst = blockinfoarr.Cast<long>().ToList();

            for (int i = 0; i < lst.Count; i += 2)
                blockInfoList.Add(new BlockInfo { extraNonce = (int)lst[i], nonce = (uint)lst[i + 1] });

            return blockInfoList;
        }
    }
}