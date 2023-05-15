using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Blockcore.Benchmark.Uint256;
using Blockcore.NBitcoin;
using uint256 = Blockcore.Benchmark.Uint256.New.uint256;

namespace Blockcore.Benchmark.Uint256
{
    [RankColumn, MemoryDiagnoser]
    public class Uint256_Serialize
    {
        private readonly byte[] data;
        private readonly New.uint256.MutableUint256 dataNew;
        private readonly Old.uint256.MutableUint256 dataOld;

        public Uint256_Serialize()
        {
            this.data = new byte[32];
            new Random(32).NextBytes(this.data);
            this.dataNew = new uint256.MutableUint256(new New.uint256(this.data));
            this.dataOld = new Old.uint256.MutableUint256(new Old.uint256(this.data));
        }

        [Benchmark]
        public New.uint256 Uint256_Serialize_New()
        {
            using (var ms = new MemoryStream(this.data))
                this.dataNew.ReadWrite(new BitcoinStream(ms, true));
            return this.dataNew.Value;
        }

        [Benchmark]
        public Old.uint256 Uint256_Serialize_Old()
        {
            using (var ms = new MemoryStream(this.data))
                this.dataOld.ReadWrite(new BitcoinStream(ms, true));
            return this.dataOld.Value;
        }

        [Benchmark]
        public New.uint256 Uint256_Deserialize_New()
        {
            using (var ms = new MemoryStream(this.data))
                this.dataNew.ReadWrite(new BitcoinStream(ms, false));
            return this.dataNew.Value;
        }

        [Benchmark]
        public Old.uint256 Uint256_Deserialize_Old()
        {
            using (var ms = new MemoryStream(this.data))
                this.dataOld.ReadWrite(new BitcoinStream(ms, false));
            return this.dataOld.Value;
        }
    }
}