using Blockcore.Interfaces;

namespace Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers
{
    public class BlockStoreAlwaysFlushCondition : IBlockStoreQueueFlushCondition
    {
        public bool ShouldFlush => true;
    }
}
