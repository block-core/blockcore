using System;
using Stratis.Bitcoin.Networks;

namespace Stratis.Bitcoin.IntegrationTests.Common.TestNetworks
{
    public sealed class BitcoinRegTestNoValidationRules : BitcoinRegTest
    {
        public BitcoinRegTestNoValidationRules(string name = null)
        {
            this.Name = name ?? Guid.NewGuid().ToString();
        }
    }
}
