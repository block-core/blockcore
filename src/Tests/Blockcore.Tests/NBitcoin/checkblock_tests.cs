using System.IO;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
    public class Checkblock_Tests
    {
        private readonly Network networkMain;

        public Checkblock_Tests()
        {
            this.networkMain = KnownNetworks.Main;
        }

        [Fact]
        public void CanCalculateMerkleRoot()
        {
            Block block = this.networkMain.CreateBlock();
            block.ReadWrite(Encoders.Hex.DecodeData(File.ReadAllText(TestDataLocations.GetFileFromDataFolder("block169482.txt"))), this.networkMain.Consensus.ConsensusFactory);
            Assert.Equal(block.Header.HashMerkleRoot, block.GetMerkleRoot().Hash);
        }
    }
}