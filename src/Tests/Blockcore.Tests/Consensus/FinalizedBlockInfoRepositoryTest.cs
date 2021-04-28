using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Consensus;
using Blockcore.Features.Base.Persistence.LevelDb;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using Blockcore.Utilities.Store;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Tests.Consensus
{
    public class FinalizedBlockInfoRepositoryTest : TestBase
    {
        private readonly ILoggerFactory loggerFactory;

        public FinalizedBlockInfoRepositoryTest() : base(KnownNetworks.StratisRegTest)
        {
            this.loggerFactory = new LoggerFactory();
        }

        [Fact]
        public async Task FinalizedHeightSavedOnDiskAsync()
        {
            string dir = CreateTestDir(this);
            var kvRepo = new LevelDbKeyValueRepository(dir, new DataStoreSerializer(this.Network.Consensus.ConsensusFactory));
            var asyncMock = new Mock<IAsyncProvider>();
            asyncMock.Setup(a => a.RegisterTask(It.IsAny<string>(), It.IsAny<Task>()));

            using (var repo = new FinalizedBlockInfoRepository(kvRepo, this.loggerFactory, asyncMock.Object))
            {
                repo.SaveFinalizedBlockHashAndHeight(uint256.One, 777);
            }

            using (var repo = new FinalizedBlockInfoRepository(kvRepo, this.loggerFactory, asyncMock.Object))
            {
                await repo.LoadFinalizedBlockInfoAsync(this.Network);
                Assert.Equal(777, repo.GetFinalizedBlockInfo().Height);
            }
        }

        [Fact]
        public async Task FinalizedHeightCantBeDecreasedAsync()
        {
            string dir = CreateTestDir(this);
            var kvRepo = new LevelDbKeyValueRepository(dir, new DataStoreSerializer(this.Network.Consensus.ConsensusFactory));
            var asyncMock = new Mock<IAsyncProvider>();
            asyncMock.Setup(a => a.RegisterTask(It.IsAny<string>(), It.IsAny<Task>()));

            using (var repo = new FinalizedBlockInfoRepository(kvRepo, this.loggerFactory, asyncMock.Object))
            {
                repo.SaveFinalizedBlockHashAndHeight(uint256.One, 777);
                repo.SaveFinalizedBlockHashAndHeight(uint256.One, 555);

                Assert.Equal(777, repo.GetFinalizedBlockInfo().Height);
            }

            using (var repo = new FinalizedBlockInfoRepository(kvRepo, this.loggerFactory, asyncMock.Object))
            {
                await repo.LoadFinalizedBlockInfoAsync(this.Network);
                Assert.Equal(777, repo.GetFinalizedBlockInfo().Height);
            }
        }
    }
}