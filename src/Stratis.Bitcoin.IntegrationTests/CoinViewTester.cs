using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.IntegrationTests
{
    public class CoinViewTester
    {
        private ICoinView coinView;
        private List<UnspentOutput> pendingCoins = new List<UnspentOutput>();
        private HashHeightPair hash;
        private int blockHeight;

        public CoinViewTester(ICoinView coinView)
        {
            this.coinView = coinView;
            this.hash = coinView.GetTipHash();
        }

        public List<(Utilities.Coins, OutPoint)> CreateCoins(int coinCount)
        {
            var tx = new Transaction();
            tx.Outputs.AddRange(Enumerable.Range(0, coinCount)
                .Select(t => new TxOut(Money.Zero, new Key()))
                .ToArray());

            List<(Utilities.Coins, OutPoint)> lst = new List<(Utilities.Coins, OutPoint)>();
            foreach (var trxo in tx.Outputs.AsIndexedOutputs())
            {
                var output = new UnspentOutput(trxo.ToOutPoint(), new Coins(0, trxo.TxOut, false));
                this.pendingCoins.Add(output);
                lst.Add((output.Coins, output.OutPoint));

            }
            return lst;
        }

        public bool Exists((Utilities.Coins Coins, OutPoint Outpoint) c)
        {
            FetchCoinsResponse result = this.coinView.FetchCoins(new[] { c.Outpoint });
            if (result.UnspentOutputs.Count == 0)
                return false;
            return result.UnspentOutputs[c.Outpoint] != null;
        }

        public void Spend((Utilities.Coins Coins, OutPoint Outpoint) c)
        {
            UnspentOutput coin = this.pendingCoins.FirstOrDefault(u => u.OutPoint == c.Outpoint);
            if (coin == null)
            {
                FetchCoinsResponse result = this.coinView.FetchCoins(new[] { c.Outpoint });
                if (result.UnspentOutputs.Count == 0)
                    throw new InvalidOperationException("Coin unavailable");

                //if (!result.UnspentOutputs[c.Outpoint].MarkAsSpent())
                //    throw new InvalidOperationException("Coin unspendable");
                this.pendingCoins.Add(result.UnspentOutputs.Values.First());
            }
            else
            {
                if (!coin.MarkAsSpent())
                    throw new InvalidOperationException("Coin unspendable");
            }
        }

        public HashHeightPair NewBlock()
        {
            this.blockHeight++;
            var newHash = new HashHeightPair(new uint256(RandomUtils.GetBytes(32)), this.blockHeight);
            this.coinView.SaveChanges(this.pendingCoins, this.hash, newHash);
            this.pendingCoins.Clear();
            this.hash = newHash;
            return newHash;
        }

        public HashHeightPair Rewind()
        {
            this.hash = this.coinView.Rewind();
            this.blockHeight--;
            return this.hash;
        }
    }
}
