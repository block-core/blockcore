using System;
using System.Text;
using System.Threading;
using Blockcore.Consensus.Chain;
using NBitcoin;

namespace Blockcore.Consensus.PerformanceCounters.ConsensusManager
{
    /// <summary>Snapshot of <see cref="ConsensusManager"/> performance.</summary>
    public class ConsensusManagerPerformanceSnapshot
    {
        private readonly ChainIndexer chainIndex;

        public ExecutionsCountAndDelay TotalConnectionTime { get; }

        public ExecutionsCountAndDelay ConnectBlockFV { get; }

        public ExecutionsCountAndDelay BlockDisconnectedSignal { get; }

        public ExecutionsCountAndDelay BlockConnectedSignal { get; }

        public ConsensusManagerPerformanceSnapshot(ChainIndexer chainIndex)
        {
            this.TotalConnectionTime = new ExecutionsCountAndDelay();
            this.ConnectBlockFV = new ExecutionsCountAndDelay();
            this.BlockDisconnectedSignal = new ExecutionsCountAndDelay();
            this.BlockConnectedSignal = new ExecutionsCountAndDelay();
            this.chainIndex = chainIndex;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine("======ConsensusManager Bench======");

            double blocksPerMinute = this.TotalConnectionTime.GetAvgExecutionTimeCountMin();
            builder.AppendLine($"Blocks per minute: {blocksPerMinute}");
            double minutesFromNowToTip = (DateTime.UtcNow - this.chainIndex.Tip.Header.BlockTime.DateTime).TotalMinutes;
            double blockTime = this.chainIndex.Network.Consensus.TargetSpacing.TotalMinutes;
            double expectedRemainingBlocks = minutesFromNowToTip / blockTime;
            if (blocksPerMinute != 0)
                builder.AppendLine($"Estimated time to full sync: {Math.Round(TimeSpan.FromMinutes(expectedRemainingBlocks / blocksPerMinute).TotalHours, 2)} hours");
            builder.AppendLine();

            builder.AppendLine($"Total connection time (FV, CHT upd, Rewind, Signaling): {this.TotalConnectionTime.GetAvgExecutionTimeMs()} ms");

            builder.AppendLine($"Block connection (FV excluding rewind): {this.ConnectBlockFV.GetAvgExecutionTimeMs()} ms");

            builder.AppendLine($"Block connected signal: {this.BlockConnectedSignal.GetAvgExecutionTimeMs()} ms");
            builder.AppendLine($"Block disconnected signal: {this.BlockDisconnectedSignal.GetAvgExecutionTimeMs()} ms");

            return builder.ToString();
        }
    }

    public class ExecutionsCountAndDelay
    {
        private int totalExecutionsCount;
        private long totalDelayTicks;

        public ExecutionsCountAndDelay()
        {
            this.totalExecutionsCount = 0;
            this.totalDelayTicks = 0;
        }

        public double GetAvgExecutionTimeCountMin()
        {
            if (this.totalDelayTicks == 0)
                return 0;

            return Math.Round(this.totalExecutionsCount / TimeSpan.FromTicks(this.totalDelayTicks).TotalMinutes, 4);
        }

        public double GetAvgExecutionTimeMs()
        {
            if (this.totalExecutionsCount == 0)
                return 0;

            return Math.Round(TimeSpan.FromTicks(this.totalDelayTicks).TotalMilliseconds / this.totalExecutionsCount, 4);
        }

        public void Increment(long elapsedTicks)
        {
            Interlocked.Increment(ref this.totalExecutionsCount);
            Interlocked.Add(ref this.totalDelayTicks, elapsedTicks);
        }
    }
}