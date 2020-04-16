using System;
using Blockcore.Configuration;
using Blockcore.Utilities;
using LevelDB;

namespace NBitcoin
{
    public class LeveldbHeaderStore : IBlockHeaderStore, IDisposable
    {
        private readonly Network network;

        /// <summary>
        /// Headers that are close to the tip
        /// </summary>
        private readonly MemoryCountCache<uint256, BlockHeader> headers;

        private readonly DB leveldb;

        private object locker;

        public LeveldbHeaderStore(Network network, DataFolder dataFolder, ChainIndexer chainIndexer)
        {
            this.network = network;
            this.ChainIndexer = chainIndexer;
            // this.headers = new Dictionary<uint256, BlockHeader>();
            this.headers = new MemoryCountCache<uint256, BlockHeader>(601);
            this.locker = new object();

            // Open a connection to a new DB and create if not found
            var options = new Options { CreateIfMissing = true };
            this.leveldb = new DB(options, dataFolder.HeadersPath);
        }

        public ChainIndexer ChainIndexer { get; }

        public BlockHeader GetHeader(ChainedHeader chainedHeader, uint256 hash)
        {
            if (this.headers.TryGetValue(hash, out BlockHeader blockHeader))
            {
                return blockHeader;
            }

            byte[] bytes = hash.ToBytes();

            lock (this.locker)
            {
                bytes = this.leveldb.Get(bytes);
            }

            if (bytes == null)
            {
                throw new ApplicationException("Header must exist if requested");
            }

            blockHeader = this.network.Consensus.ConsensusFactory.CreateBlockHeader();
            blockHeader.FromBytes(bytes, this.network.Consensus.ConsensusFactory);

            // If the header is 500 blocks behind tip or 100 blocks ahead cache it.
            if ((chainedHeader.Height > this.ChainIndexer.Height - 500) && (chainedHeader.Height <= this.ChainIndexer.Height + 100))
                this.headers.AddOrUpdate(hash, blockHeader);

            return blockHeader;
        }

        public bool StoreHeader(BlockHeader blockHeader)
        {
            ConsensusFactory consensusFactory = this.network.Consensus.ConsensusFactory;

            lock (this.locker)
            {
                this.leveldb.Put(blockHeader.GetHash().ToBytes(), blockHeader.ToBytes(consensusFactory));
            }

            return true;
        }

        public void Dispose()
        {
            this.leveldb?.Dispose();
        }
    }
}