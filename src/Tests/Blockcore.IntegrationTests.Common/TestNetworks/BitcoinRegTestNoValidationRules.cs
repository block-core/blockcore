using System;
using Blockcore.Networks;
using Blockcore.Networks.Bitcoin;

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