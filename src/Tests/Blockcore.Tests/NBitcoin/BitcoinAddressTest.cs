using System;
using Blockcore.Tests.Common;
using Xunit;

namespace NBitcoin.Tests
{
    public class BitcoinAddressTest
    {
        [Fact]
        public void ShouldThrowBase58Exception()
        {
            string key = "";
            Assert.Throws<FormatException>(() => BitcoinAddress.Create(key, KnownNetworks.Main));

            key = null;
            Assert.Throws<ArgumentNullException>(() => BitcoinAddress.Create(key, KnownNetworks.Main));
        }
    }
}