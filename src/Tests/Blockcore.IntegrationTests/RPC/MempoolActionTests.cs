using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Features.MemoryPool;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.NBitcoin;
using Xunit;

namespace Blockcore.IntegrationTests.RPC
{
    public class MempoolActionTests : BaseRPCControllerTest
    {
        [Fact]
        public async Task CanCall_GetRawMempoolAsync()
        {
            string dir = CreateTestDir(this);
            IFullNode fullNode = this.BuildServicedNode(dir);
            var controller = fullNode.NodeController<MempoolController>();

            List<uint256> result = await controller.GetRawMempool();

            Assert.NotNull(result);
        }
    }
}
