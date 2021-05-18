using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.BlockInfo;
using NBitcoin;

namespace Blockcore.Consensus.Chain
{
    public interface IChainStore
    {
        BlockHeader GetHeader(ChainedHeader chainedHeader, uint256 hash);

        bool PutHeader(BlockHeader blockHeader);

        IEnumerable<ChainData> GetChainData();

        ChainData GetChainData(int height);

        void PutChainData(IEnumerable<ChainDataItem> items);
    }

    public class ChainData : IBitcoinSerializable
    {
        public uint256 Hash;
        public byte[] Work;

        public ChainData()
        {
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.Hash);
            if (stream.Serializing)
            {
                int len = this.Work.Length;
                stream.ReadWrite(ref len);
                stream.ReadWrite(ref this.Work);
            }
            else
            {
                int len = 0;
                stream.ReadWrite(ref len);
                this.Work = new byte[len];
                stream.ReadWrite(ref this.Work);
            }
        }
    }

    public class ChainDataItem
    {
        public int Height { get; set; }

        public ChainData Data { get; set; }
    }

    public class ChainStore : IChainStore
    {
        private readonly ConcurrentDictionary<uint256, BlockHeader> headers;
        private readonly ConcurrentDictionary<int, ChainData> chainData;

        public ChainStore()
        {
            this.headers = new ConcurrentDictionary<uint256, BlockHeader>();
            this.chainData = new ConcurrentDictionary<int, ChainData>();
        }

        public BlockHeader GetHeader(ChainedHeader chainedHeader, uint256 hash)
        {
            if (!this.headers.TryGetValue(hash, out BlockHeader header))
            {
                throw new ApplicationException("Header must exist if requested");
            }

            return header;
        }

        public bool PutHeader(BlockHeader blockHeader)
        {
            return this.headers.TryAdd(blockHeader.GetHash(), blockHeader);
        }

        public IEnumerable<ChainData> GetChainData()
        {
            // Order by height and return new array with ChainData.
            return this.chainData.OrderBy(c => c.Key).Select(c => c.Value);
        }

        public ChainData GetChainData(int height)
        {
            if (!this.chainData.TryGetValue(height, out ChainData data))
            {
                throw new ApplicationException("ChainData must exist if requested");
            }

            return data;
        }

        public void PutChainData(IEnumerable<ChainDataItem> items)
        {
            foreach (ChainDataItem item in items)
                this.chainData.TryAdd(item.Height, item.Data);
        }
    }
}