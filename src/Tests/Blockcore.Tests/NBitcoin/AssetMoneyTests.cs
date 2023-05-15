using System;
using Blockcore.NBitcoin.OpenAsset;
using Xunit;

namespace NBitcoin.Tests
{
    public class AssetMoneyTests
    {
        [Fact]
        public void AssetMoneyToStringTest()
        {
            var assetId = new AssetId("8f316d9a09");
            var assetMoney = new AssetMoney(assetId, 1);

            String actual = assetMoney.ToString();
            Assert.Equal("1-8f316d9a09", actual);
        }

        [Fact]
        public void AssetMoneyMultiply()
        {
            var assetId = new AssetId("8f316d9a09");
            var assetMoney = new AssetMoney(assetId, 2);

            AssetMoney actual = assetMoney * 2;

            Assert.Equal(4, actual.Quantity);

            actual = 2 * assetMoney;
            Assert.Equal(4, actual.Quantity);
        }

        [Fact]
        public void AssetMoneyGreaterThan()
        {
            var assetId = new AssetId("8f316d9a09");
            var smallAssetMoney = new AssetMoney(assetId, 2);
            var largeAssetMoney = new AssetMoney(assetId, 5);

            Assert.True(largeAssetMoney > smallAssetMoney);
            Assert.False(smallAssetMoney > largeAssetMoney);
        }

        [Fact]
        public void AssetMoneyLessThan()
        {
            var assetId = new AssetId("8f316d9a09");
            var smallAssetMoney = new AssetMoney(assetId, 2);
            var largeAssetMoney = new AssetMoney(assetId, 5);

            Assert.True(smallAssetMoney < largeAssetMoney);
            Assert.False(largeAssetMoney < smallAssetMoney);
        }
    }
}