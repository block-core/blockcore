using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class SpendingDetailsTest
    {
        [Fact]
        public void IsSpentConfirmedHavingBlockHeightReturnsTrue()
        {
            var spendingDetails = new SpendingDetails
            {
                BlockHeight = 15
            };

            Assert.True(spendingDetails.IsSpentConfirmed());
        }

        [Fact]
        public void IsConfirmedHavingNoBlockHeightReturnsFalse()
        {
            var spendingDetails = new SpendingDetails
            {
                BlockHeight = null
            };

            Assert.False(spendingDetails.IsSpentConfirmed());
        }
    }
}
