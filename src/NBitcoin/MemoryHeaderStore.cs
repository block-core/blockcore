using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;

namespace NBitcoin
{
    public interface IBlockHeaderStore
    {
        BlockHeader GetHeader(ChainedHeader chainedHeader, uint256 hash);

        bool StoreHeader(BlockHeader blockHeader);
    }

    public class MemoryHeaderStore : IBlockHeaderStore
    {
        private readonly ConcurrentDictionary<uint256, BlockHeader> headers;

        public MemoryHeaderStore()
        {
            this.headers = new ConcurrentDictionary<uint256, BlockHeader>();
        }

        public BlockHeader GetHeader(ChainedHeader chainedHeader, uint256 hash)
        {
            if (!this.headers.TryGetValue(hash, out BlockHeader header))
            {
                throw new ApplicationException("Header must exist if requested");
            }

            return header;
        }

        public bool StoreHeader(BlockHeader blockHeader)
        {
            return this.headers.TryAdd(blockHeader.GetHash(), blockHeader);
        }
    }
}