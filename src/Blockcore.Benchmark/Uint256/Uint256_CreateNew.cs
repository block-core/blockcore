using System;
using BenchmarkDotNet.Attributes;
using Blockcore.Benchmark.Uint256;

namespace Blockcore.Benchmark.Uint256
{
    [RankColumn, MemoryDiagnoser]
    public class Uint256_CreateNew
    {
        private readonly byte[] data;

        public Uint256_CreateNew()
        {
            this.data = new byte[32];
            new Random(32).NextBytes(this.data);
        }

        [Benchmark]
        public New.uint256 Uint256_New()
        {
            return new New.uint256(this.data);
        }

        [Benchmark]
        public Old.uint256 Uint256_Old()
        {
            return new Old.uint256(this.data);
        }

        [Benchmark]
        public New.uint256 Uint256_New_le()
        {
            return new New.uint256(this.data, false);
        }

        [Benchmark]
        public Old.uint256 Uint256_Old_le()
        {
            return new Old.uint256(this.data, false);
        }
    }
}