using System.Linq;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Utilities;
using Xunit;

namespace Stratis.Bitcoin.Features.Consensus.Tests.CoinViews
{
    public class FetchCoinsResponseTest
    {
        [Fact]
        public void Constructor_InitializesClass()
        {
            var blockHash = new uint256(124);
            var unspentOutputs = new UnspentOutput[] {
                new UnspentOutput(new OutPoint(new Transaction(), 0), new Coins(1, new TxOut(), false)),
                new UnspentOutput(new OutPoint(new Transaction(), 1), new Coins(2, new TxOut(), false))
            };

            var fetchCoinsResponse = new FetchCoinsResponse();
            fetchCoinsResponse.UnspentOutputs.Add(unspentOutputs[0].OutPoint, unspentOutputs[0]);
            fetchCoinsResponse.UnspentOutputs.Add(unspentOutputs[1].OutPoint, unspentOutputs[1]);

            Assert.Equal(2, fetchCoinsResponse.UnspentOutputs.Count);
            Assert.Equal((uint)1, fetchCoinsResponse.UnspentOutputs.ToList()[0].Value.Coins.Height);
            Assert.Equal((uint)2, fetchCoinsResponse.UnspentOutputs.ToList()[1].Value.Coins.Height);
        }
    }
}
