using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Configuration;
using Blockcore.Configuration.Logging;
using Blockcore.Configuration.Settings;
using Blockcore.Consensus;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.Checkpoints;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Features.Consensus.Persistence.LevelDb;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Networks;
using Blockcore.Networks.Stratis;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

using Xunit;

namespace Blockcore.Features.Consensus.Tests.CoinViews
{
    public class CoinviewTests
    {
        private readonly Network network;
        private readonly DataFolder dataFolder;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly INodeStats nodeStats;
        private readonly ICoindb coindb;

        private readonly ChainIndexer chainIndexer;
        private readonly StakeChainStore stakeChainStore;
        private readonly IRewindDataIndexCache rewindDataIndexCache;
        private readonly CachedCoinView cachedCoinView;
        private readonly Random random;

        public CoinviewTests()
        {
            this.network = new StratisMain();
            this.dataFolder = TestBase.CreateDataFolder(this);
            this.dateTimeProvider = new DateTimeProvider();
            this.loggerFactory = new ExtendedLoggerFactory();
            this.nodeStats = new NodeStats(this.dateTimeProvider, this.loggerFactory);

            //this.coindb = new DBreezeCoindb(this.network, this.dataFolder, this.dateTimeProvider, this.loggerFactory, this.nodeStats, new DataStoreSerializer(this.network.Consensus.ConsensusFactory));
            //this.coindb = new FasterCoindb(this.network, this.dataFolder, this.dateTimeProvider, this.loggerFactory, this.nodeStats, new DataStoreSerializer(this.network.Consensus.ConsensusFactory));
            this.coindb = new LevelDbCoindb(this.network, this.dataFolder, this.dateTimeProvider, this.loggerFactory, this.nodeStats, new DataStoreSerializer(this.network.Consensus.ConsensusFactory));
            this.coindb.Initialize();

            this.chainIndexer = new ChainIndexer(this.network);
            this.stakeChainStore = new StakeChainStore(this.network, this.chainIndexer, (IStakdb)this.coindb, this.loggerFactory);
            this.stakeChainStore.Load();

            this.rewindDataIndexCache = new RewindDataIndexCache(this.dateTimeProvider, this.network, new FinalizedBlockInfoRepository(new HashHeightPair()), new Checkpoints());

            this.cachedCoinView = new CachedCoinView(this.network, new Checkpoints(), this.coindb, this.dateTimeProvider, this.loggerFactory, this.nodeStats, new ConsensusSettings(new NodeSettings(this.network)), this.stakeChainStore, this.rewindDataIndexCache);

            this.rewindDataIndexCache.Initialize(this.chainIndexer.Height, this.cachedCoinView);

            this.random = new Random();

            ChainedHeader newTip = ChainedHeadersHelper.CreateConsecutiveHeaders(1000, this.chainIndexer.Tip, true, null, this.network).Last();
            this.chainIndexer.SetTip(newTip);
        }

        [Fact]
        public async Task TestRewindAsync()
        {
            HashHeightPair tip = this.cachedCoinView.GetTipHash();
            Assert.Equal(this.chainIndexer.Genesis.HashBlock, tip.Hash);

            int currentHeight = 0;

            // Create a lot of new coins.
            List<UnspentOutput> outputsList = this.CreateOutputsList(currentHeight + 1, 100);
            this.SaveChanges(outputsList, currentHeight + 1);
            currentHeight++;

            this.cachedCoinView.Flush(true);

            HashHeightPair tipAfterOriginalCoinsCreation = this.cachedCoinView.GetTipHash();

            // Collection that will be used as a coinview that we will update in parallel. Needed to verify that actual coinview is ok.
            List<OutPoint> outPoints = this.ConvertToListOfOutputPoints(outputsList);

            // Copy of current state to later rewind and verify against it.
            List<OutPoint> copyOfOriginalOutPoints = new List<OutPoint>(outPoints);

            List<OutPoint> copyAfterHalfOfAdditions = new List<OutPoint>();
            HashHeightPair coinviewTipAfterHalf = null;

            int addChangesTimes = 500;
            // Spend some coins in the next N saves.
            for (int i = 0; i < addChangesTimes; ++i)
            {
                OutPoint txId = outPoints[this.random.Next(0, outPoints.Count)];
                List<OutPoint> txPoints = outPoints.Where(x => x.Hash == txId.Hash).ToList();
                this.Shuffle(txPoints);
                List<OutPoint> txPointsToSpend = txPoints.Take(txPoints.Count / 2).ToList();

                // First spend in cached coinview
                FetchCoinsResponse response = this.cachedCoinView.FetchCoins(txPoints.ToArray());
                Assert.Equal(txPoints.Count, response.UnspentOutputs.Count);
                var toSpend = new List<UnspentOutput>();
                foreach (OutPoint outPointToSpend in txPointsToSpend)
                {
                    response.UnspentOutputs[outPointToSpend].Spend();
                    toSpend.Add(response.UnspentOutputs[outPointToSpend]);
                }

                // Spend from outPoints.
                outPoints.RemoveAll(x => txPointsToSpend.Contains(x));

                // Save coinview
                this.SaveChanges(toSpend, currentHeight + 1);

                currentHeight++;

                if (i == addChangesTimes / 2)
                {
                    copyAfterHalfOfAdditions = new List<OutPoint>(outPoints);
                    coinviewTipAfterHalf = this.cachedCoinView.GetTipHash();
                }
            }

            await this.ValidateCoinviewIntegrityAsync(outPoints);

            for (int i = 0; i < addChangesTimes; i++)
            {
                this.cachedCoinView.Rewind();

                HashHeightPair currentTip = this.cachedCoinView.GetTipHash();

                if (currentTip == coinviewTipAfterHalf)
                    await this.ValidateCoinviewIntegrityAsync(copyAfterHalfOfAdditions);
            }

            Assert.Equal(tipAfterOriginalCoinsCreation, this.cachedCoinView.GetTipHash());

            await this.ValidateCoinviewIntegrityAsync(copyOfOriginalOutPoints);
        }

        private List<OutPoint> ConvertToListOfOutputPoints(List<UnspentOutput> outputsList)
        {
            return outputsList.Select(s => s.OutPoint).ToList();
        }

        private List<UnspentOutput> CreateOutputsList(int height, int itemsCount = 10)
        {
            List<UnspentOutput> lst = new List<UnspentOutput>();

            for (int j = 0; j < itemsCount; j++)
            {
                int outputCount = 20;
                var tx = new Transaction();
                tx.LockTime = RandomUtils.GetUInt32(); // add randmoness

                for (int i = 0; i < outputCount; i++)
                {
                    var money = new Money(this.random.Next(1_000, 1_000_000));
                    tx.AddOutput(money, Script.Empty);
                }

                foreach (IndexedTxOut txout in tx.Outputs.AsIndexedOutputs())
                {
                    lst.Add(new UnspentOutput(txout.ToOutPoint(), new Coins((uint)height, txout.TxOut, false)));
                }
            }

            return lst;
        }

        private void SaveChanges(List<UnspentOutput> unspent, int height)
        {
            ChainedHeader current = this.chainIndexer.Tip.GetAncestor(height);
            ChainedHeader previous = current.Previous;

            this.cachedCoinView.SaveChanges(unspent, new HashHeightPair(previous), new HashHeightPair(current));
        }

        private Task ValidateCoinviewIntegrityAsync(List<OutPoint> expectedAvailableOutPoints)
        {
            FetchCoinsResponse result = this.cachedCoinView.FetchCoins(expectedAvailableOutPoints.ToArray());

            foreach (OutPoint outPoints in expectedAvailableOutPoints)
            {
                // Check unexpected coins are not present.
                Assert.NotNull(result.UnspentOutputs[outPoints].Coins);
            }

            // Verify that snapshot is equal to current state of coinview.
            OutPoint[] allTxIds = expectedAvailableOutPoints.Select(x => x).Distinct().ToArray();
            FetchCoinsResponse result2 = this.cachedCoinView.FetchCoins(allTxIds);
            List<OutPoint> availableOutPoints = this.ConvertToListOfOutputPoints(result2.UnspentOutputs.Values.ToList());

            Assert.Equal(expectedAvailableOutPoints.Count, availableOutPoints.Count);

            foreach (OutPoint referenceOutPoint in expectedAvailableOutPoints)
            {
                Assert.Contains(referenceOutPoint, availableOutPoints);
            }

            return Task.CompletedTask;
        }

        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = this.random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}