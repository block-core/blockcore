using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Configuration.Logging;
using Stratis.Bitcoin.Configuration.Settings;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Consensus.ProvenBlockHeaders;
using Stratis.Bitcoin.Networks;
using Stratis.Bitcoin.Tests.Common;
using Stratis.Bitcoin.Utilities;
using Xunit;

namespace Stratis.Bitcoin.Features.Consensus.Tests.CoinViews
{
    public class CoinviewTests
    {
        private readonly Network network;
        private readonly DataFolder dataFolder;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly INodeStats nodeStats;
        private readonly DBreezeCoinView dbreezeCoinview;

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

            this.dbreezeCoinview = new DBreezeCoinView(this.network, this.dataFolder, this.dateTimeProvider, this.loggerFactory, this.nodeStats, new DBreezeSerializer(this.network.Consensus.ConsensusFactory));
            this.dbreezeCoinview.Initialize();

            this.chainIndexer = new ChainIndexer(this.network);
            this.stakeChainStore = new StakeChainStore(this.network, this.chainIndexer, this.dbreezeCoinview, this.loggerFactory);
            this.stakeChainStore.Load();

            this.rewindDataIndexCache = new RewindDataIndexCache(this.dateTimeProvider, this.network, new FinalizedBlockInfoRepository(new HashHeightPair()) , new Checkpoints());

            this.cachedCoinView = new CachedCoinView(this.network, new Checkpoints(),  this.dbreezeCoinview, this.dateTimeProvider, this.loggerFactory, this.nodeStats, new ConsensusSettings(new NodeSettings(this.network)) , this.stakeChainStore, this.rewindDataIndexCache);

            this.rewindDataIndexCache.Initialize(this.chainIndexer.Height, this.cachedCoinView);

            this.random = new Random();

            ChainedHeader newTip = ChainedHeadersHelper.CreateConsecutiveHeaders(1000, this.chainIndexer.Tip, true, null, this.network).Last();
            this.chainIndexer.SetTip(newTip);
        }

        [Fact]
        public async Task TestRewindAsync()
        {
            uint256 tip = this.cachedCoinView.GetTipHash().Hash;
            Assert.Equal(this.chainIndexer.Genesis.HashBlock, tip);

            int currentHeight = 0;

            // Create a lot of new coins.
            List<UnspentOutput> outputsList = this.CreateOutputsList(currentHeight + 1, 100);
            this.SaveChanges(outputsList, currentHeight + 1);
            currentHeight++;

            this.cachedCoinView.Flush(true);

            uint256 tipAfterOriginalCoinsCreation = this.cachedCoinView.GetTipHash().Hash;

            // Collection that will be used as a coinview that we will update in parallel. Needed to verify that actual coinview is ok.
            List<OutPoint> outPoints = this.ConvertToListOfOutputPoints(outputsList);

            // Copy of current state to later rewind and verify against it.
            List<OutPoint> copyOfOriginalOutPoints = new List<OutPoint>(outPoints);

            List<OutPoint> copyAfterHalfOfAdditions = new List<OutPoint>();
            uint256 coinviewTipAfterHalf = null;

            int addChangesTimes = 500;
            // Spend some coins in the next N saves.
            for (int i = 0; i < addChangesTimes; ++i)
            {
                OutPoint txId = outPoints[this.random.Next(0, outPoints.Count)];
                List<OutPoint> txPoints = outPoints.Where(x => x.Hash == txId.Hash).ToList();
                this.Shuffle(txPoints);
                List<OutPoint> txPointsToSpend = txPoints.Take(txPoints.Count / 2).ToList();

                // First spend in cached coinview
                FetchCoinsResponse response = this.cachedCoinView.FetchCoins(new[] { txId });
                Assert.Single(response.UnspentOutputs);

                UnspentOutput coins = response.UnspentOutputs.Values.First(); ;
                UnspentOutput unchangedClone = new UnspentOutput(coins.OutPoint, coins.Coins);

                foreach (OutPoint outPointToSpend in txPointsToSpend)
                    coins.Spend();

                // Spend from outPoints.
                outPoints.RemoveAll(x => txPointsToSpend.Contains(x));

                // Save coinview
                this.SaveChanges(new List<UnspentOutput>() { coins }, currentHeight + 1);

                currentHeight++;

                if (i == addChangesTimes / 2)
                {
                    copyAfterHalfOfAdditions = new List<OutPoint>(outPoints);
                    coinviewTipAfterHalf = this.cachedCoinView.GetTipHash().Hash;
                }
            }

            await this.ValidateCoinviewIntegrityAsync(outPoints);

            for (int i = 0; i < addChangesTimes; i++)
            {
                this.cachedCoinView.Rewind();

                uint256 currentTip = this.cachedCoinView.GetTipHash().Hash;

                if (currentTip == coinviewTipAfterHalf)
                    await this.ValidateCoinviewIntegrityAsync(copyAfterHalfOfAdditions);
            }

            Assert.Equal(tipAfterOriginalCoinsCreation, this.cachedCoinView.GetTipHash().Hash);

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
                    lst.Add(new UnspentOutput(new OutPoint(tx, i), new Coins((uint)height, tx.AddOutput(money, Script.Empty), false)));
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

        private async Task ValidateCoinviewIntegrityAsync(List<OutPoint> expectedAvailableOutPoints)
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
