using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Base;
using Blockcore.Configuration;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Base.Persistence.LevelDb;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using DBreeze.DataTypes;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Crypto;
using Xunit;

namespace Blockcore.IntegrationTests
{
    public class CoinViewTests
    {
        protected readonly ILoggerFactory loggerFactory;
        private readonly Network network;
        private readonly Network regTest;
        private readonly DataStoreSerializer dataStoreSerializer;

        /// <summary>
        /// Initializes logger factory for tests in this class.
        /// </summary>
        public CoinViewTests()
        {
            this.loggerFactory = new LoggerFactory();
            this.network = KnownNetworks.Main;
            this.regTest = KnownNetworks.RegTest;
            this.dataStoreSerializer = new DataStoreSerializer(this.network.Consensus.ConsensusFactory);
        }

        [Fact]
        public void TestDBreezeSerialization()
        {
            using (NodeContext ctx = NodeContext.Create(this))
            {
                Block genesis = ctx.Network.GetGenesis();
                var genesisChainedHeader = new ChainedHeader(genesis.Header, ctx.Network.GenesisHash, 0);
                ChainedHeader chained = this.MakeNext(genesisChainedHeader, ctx.Network);
                ctx.Coindb.SaveChanges(new UnspentOutput[] { new UnspentOutput(new OutPoint(genesis.Transactions[0], 0), new Coins(0, genesis.Transactions[0].Outputs.First(), true)) }, new HashHeightPair(genesisChainedHeader), new HashHeightPair(chained));
                Assert.NotNull(ctx.Coindb.FetchCoins(new[] { new OutPoint(genesis.Transactions[0], 0) }).UnspentOutputs.Values.FirstOrDefault().Coins);
                Assert.Null(ctx.Coindb.FetchCoins(new[] { new OutPoint() }).UnspentOutputs.Values.FirstOrDefault().Coins);

                ChainedHeader previous = chained;
                chained = this.MakeNext(this.MakeNext(genesisChainedHeader, ctx.Network), ctx.Network);
                chained = this.MakeNext(this.MakeNext(genesisChainedHeader, ctx.Network), ctx.Network);
                ctx.Coindb.SaveChanges(new List<UnspentOutput>(), new HashHeightPair(previous), new HashHeightPair(chained));
                Assert.Equal(chained.HashBlock, ctx.Coindb.GetTipHash().Hash);
                ctx.ReloadPersistentCoinView();
                Assert.Equal(chained.HashBlock, ctx.Coindb.GetTipHash().Hash);
                Assert.NotNull(ctx.Coindb.FetchCoins(new[] { new OutPoint(genesis.Transactions[0], 0) }).UnspentOutputs.Values.FirstOrDefault().Coins);
                Assert.Null(ctx.Coindb.FetchCoins(new[] { new OutPoint() }).UnspentOutputs.Values.FirstOrDefault().Coins);
            }
        }

        [Fact]
        public void TestCacheCoinView()
        {
            using (NodeContext ctx = NodeContext.Create(this))
            {
                Block genesis = ctx.Network.GetGenesis();
                var genesisChainedHeader = new ChainedHeader(genesis.Header, ctx.Network.GenesisHash, 0);
                ChainedHeader chained = this.MakeNext(genesisChainedHeader, ctx.Network);
                var dateTimeProvider = new DateTimeProvider();

                var cacheCoinView = new CachedCoinView(this.network, new Checkpoints(), ctx.Coindb, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), new ConsensusSettings(new NodeSettings(this.network)));

                cacheCoinView.SaveChanges(new UnspentOutput[] { new UnspentOutput(new OutPoint(genesis.Transactions[0], 0), new Coins(0, genesis.Transactions[0].Outputs.First(), true)) }, new HashHeightPair(genesisChainedHeader), new HashHeightPair(chained));
                Assert.NotNull(cacheCoinView.FetchCoins(new[] { new OutPoint(genesis.Transactions[0], 0) }).UnspentOutputs.Values.FirstOrDefault().Coins);
                Assert.Null(cacheCoinView.FetchCoins(new[] { new OutPoint() }).UnspentOutputs.Values.FirstOrDefault().Coins);
                Assert.Equal(new HashHeightPair(chained), cacheCoinView.GetTipHash());

                Assert.Null(ctx.Coindb.FetchCoins(new[] { new OutPoint(genesis.Transactions[0], 0) }).UnspentOutputs.Values.FirstOrDefault().Coins);
                Assert.Equal(chained.Previous.HashBlock, ctx.Coindb.GetTipHash().Hash);
                cacheCoinView.Flush();
                Assert.NotNull(ctx.Coindb.FetchCoins(new[] { new OutPoint(genesis.Transactions[0], 0) }).UnspentOutputs.Values.FirstOrDefault().Coins);
                Assert.Equal(chained.HashBlock, ctx.Coindb.GetTipHash().Hash);
                //Assert.Null(ctx.PersistentCoinView.FetchCoinsAsync(new[] { new uint256() }).Result.UnspentOutputs[0]);

                //var previous = chained;
                //chained = MakeNext(MakeNext(genesisChainedBlock));
                //chained = MakeNext(MakeNext(genesisChainedBlock));
                //ctx.PersistentCoinView.SaveChangesAsync(new UnspentOutputs[0], previous.HashBlock, chained.HashBlock).Wait();
                //Assert.Equal(chained.HashBlock, ctx.PersistentCoinView.GetTipHashAsync().GetAwaiter().GetResult());
                //ctx.ReloadPersistentCoinView();
                //Assert.Equal(chained.HashBlock, ctx.PersistentCoinView.GetTipHashAsync().GetAwaiter().GetResult());
                //Assert.NotNull(ctx.PersistentCoinView.FetchCoinsAsync(new[] { genesis.Transactions[0].GetHash() }).Result.UnspentOutputs[0]);
                //Assert.Null(ctx.PersistentCoinView.FetchCoinsAsync(new[] { new uint256() }).Result.UnspentOutputs[0]);
            }
        }

        [Fact]
        public void CanRewind()
        {
            using (NodeContext nodeContext = NodeContext.Create(this))
            {
                var dateTimeProvider = new DateTimeProvider();
                var cacheCoinView = new CachedCoinView(this.network, new Checkpoints(), nodeContext.Coindb, dateTimeProvider, this.loggerFactory, new NodeStats(dateTimeProvider, this.loggerFactory), new ConsensusSettings(new NodeSettings(this.network)));
                var tester = new CoinViewTester(cacheCoinView);

                List<(Coins, OutPoint)> coinsA = tester.CreateCoins(5);
                List<(Coins, OutPoint)> coinsB = tester.CreateCoins(1);
                tester.NewBlock();
                cacheCoinView.Flush();
                Assert.True(tester.Exists(coinsA[2]));
                Assert.True(tester.Exists(coinsB[0]));

                // Spend some coins.
                tester.Spend(coinsA[2]);
                tester.Spend(coinsB[0]);

                tester.NewBlock();

                // This will save an empty RewindData instance
                tester.NewBlock();

                // Create a new coin set/
                List<(Coins, OutPoint)> coinsC = tester.CreateCoins(1);
                tester.NewBlock();
                Assert.True(tester.Exists(coinsA[0]));
                Assert.True(tester.Exists(coinsC[0]));
                Assert.False(tester.Exists(coinsA[2]));
                Assert.False(tester.Exists(coinsB[0]));

                // We need to rewind 3 times as we are now rewinding one block at a time.
                tester.Rewind(); // coinsC[0] should not exist any more.
                tester.Rewind(); // coinsA[2] should be spendable again.
                tester.Rewind(); // coinsB[2] should be spendable again.
                Assert.False(tester.Exists(coinsC[0]));
                Assert.True(tester.Exists(coinsA[2]));
                Assert.True(tester.Exists(coinsB[0]));

                // Spend some coins and esnure they are not spendable.
                tester.Spend(coinsA[2]);
                tester.Spend(coinsB[0]);
                tester.NewBlock();
                cacheCoinView.Flush();
                Assert.False(tester.Exists(coinsA[2]));
                Assert.False(tester.Exists(coinsB[0]));

                // Rewind so that coinsA[2] and coinsB[0] become spendable again.
                tester.Rewind();
                Assert.True(tester.Exists(coinsA[2]));
                Assert.True(tester.Exists(coinsB[0]));

                // Create 7 coins in a new coin set and spend the first coin.
                List<(Coins, OutPoint)> coinsD = tester.CreateCoins(7);
                tester.Spend(coinsD[0]);
                // Create a coin in a new coin set and spend it.
                List<(Coins, OutPoint)> coinsE = tester.CreateCoins(1);
                tester.Spend(coinsE[0]);
                tester.NewBlock();

                Assert.True(tester.Exists(coinsD[1]));
                Assert.False(tester.Exists(coinsD[0]));
                cacheCoinView.Flush();

                // Creates another empty RewindData instance.
                tester.NewBlock();

                // Rewind one block.
                tester.Rewind();

                // coinsD[1] was never touched, so should remain unchanged.
                // coinsD[0] was spent but the block in which the changes happened was not yet rewound to, so it remains unchanged.
                // coinsE[0] was spent but the block in which the changes happened was not yet rewound to, so it remains unchanged.
                // coinsA[1] was not touched, so should remain unchanged.
                // coinsB[1] was not touched, so should remain unchanged.
                Assert.True(tester.Exists(coinsD[1]));
                Assert.False(tester.Exists(coinsD[0]));
                Assert.False(tester.Exists(coinsE[0]));
                Assert.True(tester.Exists(coinsA[2]));
                Assert.True(tester.Exists(coinsB[0]));

                // Rewind one block.
                tester.Rewind();

                // coinsD[0] should now not exist in CoinView anymore.
                // coinsE[0] should now not exist in CoinView anymore.
                Assert.False(tester.Exists(coinsD[0]));
                Assert.False(tester.Exists(coinsE[0]));
            }
        }

        [Fact]
        [Trait("Unstable", "True")]
        public void CanHandleReorgs()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode stratisNode = builder.CreateStratisPowNode(this.regTest, "cv-1-stratisNode").Start();
                CoreNode coreNode1 = builder.CreateBitcoinCoreNode().Start();
                CoreNode coreNode2 = builder.CreateBitcoinCoreNode().Start();

                //Core1 discovers 10 blocks, sends to stratis
                coreNode1.FindBlock(10).Last();
                TestHelper.ConnectAndSync(stratisNode, coreNode1);
                TestHelper.Disconnect(stratisNode, coreNode1);

                //Core2 discovers 20 blocks, sends to stratis
                coreNode2.FindBlock(20).Last();
                TestHelper.ConnectAndSync(stratisNode, coreNode2);
                TestHelper.Disconnect(stratisNode, coreNode2);
                ((CachedCoinView)stratisNode.FullNode.CoinView()).Flush();

                //Core1 discovers 30 blocks, sends to stratis
                coreNode1.FindBlock(30).Last();
                TestHelper.ConnectAndSync(stratisNode, coreNode1);
                TestHelper.Disconnect(stratisNode, coreNode1);

                //Core2 discovers 50 blocks, sends to stratis
                coreNode2.FindBlock(50).Last();
                TestHelper.ConnectAndSync(stratisNode, coreNode2);
                TestHelper.Disconnect(stratisNode, coreNode2);
                ((CachedCoinView)stratisNode.FullNode.CoinView()).Flush();

                TestBase.WaitLoop(() => TestHelper.AreNodesSynced(stratisNode, coreNode2));
            }
        }

        [Fact]
        [Trait("Unstable", "True")]
        public void TestDBreezeInsertOrder()
        {
            using (NodeContext ctx = NodeContext.Create(this))
            {
                using (var engine = new DBreeze.DBreezeEngine(ctx.FolderName + "/2"))
                {
                    var data = new[]
                    {
                        new uint256(3),
                        new uint256(2),
                        new uint256(2439425),
                        new uint256(5),
                        new uint256(243945),
                        new uint256(10),
                        new uint256(Hashes.Hash256(new byte[0])),
                        new uint256(Hashes.Hash256(new byte[] {1})),
                        new uint256(Hashes.Hash256(new byte[] {2})),
                    };
                    Array.Sort(data, new UInt256Comparer());

                    using (DBreeze.Transactions.Transaction tx = engine.GetTransaction())
                    {
                        foreach (uint256 d in data)
                            tx.Insert("Table", d.ToBytes(false), d.ToBytes());
                        tx.Commit();
                    }

                    var data2 = new uint256[data.Length];
                    using (DBreeze.Transactions.Transaction tx = engine.GetTransaction())
                    {
                        int i = 0;
                        foreach (Row<byte[], byte[]> row in tx.SelectForward<byte[], byte[]>("Table"))
                        {
                            data2[i++] = new uint256(row.Key, false);
                        }
                    }

                    Assert.True(data.SequenceEqual(data2));
                }
            }
        }

        private ChainedHeader MakeNext(ChainedHeader previous, Network network)
        {
            BlockHeader header = BlockHeader.Load(previous.Header.ToBytes(network.Consensus.ConsensusFactory), network);
            header.HashPrevBlock = previous.HashBlock;
            return new ChainedHeader(header, header.GetHash(), previous);
        }

        [Fact]
        public void CanSaveChainIncrementally()
        {
            var chain = new ChainIndexer(this.regTest);
            var data = new DataFolder(TestBase.CreateTestDir(this));

            using (var repo = new ChainRepository(this.loggerFactory, new LevelDbChainStore(this.network, data, chain), this.network))
            {
                chain.SetTip(repo.LoadAsync(chain.Genesis).GetAwaiter().GetResult());
                Assert.True(chain.Tip == chain.Genesis);
                chain = new ChainIndexer(this.regTest);
                ChainedHeader tip = this.AppendBlock(chain);
                repo.SaveAsync(chain).GetAwaiter().GetResult();
                var newChain = new ChainIndexer(this.regTest);
                newChain.SetTip(repo.LoadAsync(chain.Genesis).GetAwaiter().GetResult());
                Assert.Equal(tip, newChain.Tip);
                tip = this.AppendBlock(chain);
                repo.SaveAsync(chain).GetAwaiter().GetResult();
                newChain = new ChainIndexer(this.regTest);
                newChain.SetTip(repo.LoadAsync(chain.Genesis).GetAwaiter().GetResult());
                Assert.Equal(tip, newChain.Tip);
            }
        }

        public ChainedHeader AppendBlock(ChainedHeader previous, params ChainIndexer[] chainsIndexer)
        {
            ChainedHeader last = null;
            uint nonce = RandomUtils.GetUInt32();
            foreach (ChainIndexer chain in chainsIndexer)
            {
                Block block = this.network.CreateBlock();
                block.AddTransaction(this.network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.HashPrevBlock = previous == null ? chain.Tip.HashBlock : previous.HashBlock;
                block.Header.Nonce = nonce;
                if (!chain.TrySetTip(block.Header, out last))
                    throw new InvalidOperationException("Previous not existing");
            }
            return last;
        }

        private ChainedHeader AppendBlock(params ChainIndexer[] chainsIndexer)
        {
            ChainedHeader index = null;
            return this.AppendBlock(index, chainsIndexer);
        }
    }
}