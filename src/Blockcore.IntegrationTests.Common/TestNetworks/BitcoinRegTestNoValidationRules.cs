using System;
using Blockcore.Networks;

namespace Blockcore.IntegrationTests.Common.TestNetworks
{
    public sealed class BitcoinRegTestNoValidationRules : BitcoinRegTest
    {
        public BitcoinRegTestNoValidationRules(string name = null)
        {
            this.Name = name ?? Guid.NewGuid().ToString();
        }
    }
}
