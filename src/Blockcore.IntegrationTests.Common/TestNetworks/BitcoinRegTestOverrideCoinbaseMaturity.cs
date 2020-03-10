using System;
using Blockcore.Networks;

namespace Blockcore.IntegrationTests.Common.TestNetworks
{
    public class BitcoinRegTestOverrideCoinbaseMaturity : BitcoinRegTest
    {
        public BitcoinRegTestOverrideCoinbaseMaturity(int coinbaseMaturity, string name = null) : base()
        {
            this.Name = name ?? Guid.NewGuid().ToString();
            this.Consensus.CoinbaseMaturity = coinbaseMaturity;
        }
    }
}
