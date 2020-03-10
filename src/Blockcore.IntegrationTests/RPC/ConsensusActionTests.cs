using NBitcoin;
using Blockcore.IntegrationTests.Common;
using Blockcore.Features.Consensus;
using Blockcore.Interfaces;
using Xunit;
using Blockcore.Base;

namespace Blockcore.IntegrationTests.RPC
{
    public class ConsensusActionTests : BaseRPCControllerTest
    {
        [Fact]
        public void CanCall_GetBestBlockHash()
        {
            string dir = CreateTestDir(this);

            IFullNode fullNode = this.BuildServicedNode(dir);
            var controller = fullNode.NodeController<ConsensusRPCController>();

            uint256 result = controller.GetBestBlockHash();

            Assert.Null(result);
        }

        [Fact]
        public void CanCall_GetBlockHash()
        {
            string dir = CreateTestDir(this);

            IFullNode fullNode = this.BuildServicedNode(dir);
            var controller = fullNode.NodeController<ConsensusRPCController>();

            uint256 result = controller.GetBlockHash(0);

            Assert.Null(result);
        }

        [Fact]
        public void CanCall_IsInitialBlockDownload()
        {
            string dir = CreateTestDir(this);

            IFullNode fullNode = this.BuildServicedNode(dir);
            var isIBDProvider = fullNode.NodeService<IInitialBlockDownloadState>(true);
            var chainState = fullNode.NodeService<IChainState>(true);
            chainState.ConsensusTip = new ChainedHeader(fullNode.Network.GetGenesis().Header, fullNode.Network.GenesisHash, 0);

            Assert.NotNull(isIBDProvider);
            Assert.True(isIBDProvider.IsInitialBlockDownload());
        }
    }
}
