using System;
using BenchmarkDotNet.Attributes;
using Blockcore.Benchmark.Uint256;
using Blockcore.Benchmark.Uint256.New;

namespace Blockcore.Benchmark.Uint256
{
    [RankColumn, MemoryDiagnoser]
    public class Uint256_ToBytes
    {
        private readonly New.uint256 dataNew;
        private readonly Old.uint256 dataOld;

        public Uint256_ToBytes()
        {
            var data = new byte[32];
            new Random(32).NextBytes(data);

            this.dataNew = new New.uint256(data);
            this.dataOld = new Old.uint256(data);
        }

        [Benchmark]
        public byte[] Uint256_New()
        {
            return this.dataNew.ToBytes();
        }

        [Benchmark]
        public byte[] Uint256_Old()
        {
            return this.dataOld.ToBytes();
        }

        [Benchmark]
        public byte[] Uint256_New_le()
        {
            return this.dataNew.ToBytes(false);
        }

        [Benchmark]
        public byte[] Uint256_Old_le()
        {
            return this.dataOld.ToBytes(false);
        }
    }
}