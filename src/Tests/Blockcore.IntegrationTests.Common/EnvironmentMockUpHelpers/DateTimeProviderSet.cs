using System;
using Blockcore.Utilities;

namespace Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers
{
    public class DateTimeProviderSet : DateTimeProvider
    {
        public long time;
        public DateTime timeutc;

        public override long GetTime()
        {
            return this.time;
        }

        public override DateTime GetUtcNow()
        {
            return this.timeutc;
        }
    }
}
