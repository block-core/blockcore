using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Networks;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Consensus.Chain
{
    public interface IChainRepository : IDisposable
    {
        /// <summary>Loads the chain of headers from the database.</summary>
        /// <returns>Tip of the loaded chain.</returns>
        Task<ChainedHeader> LoadAsync(ChainedHeader genesisHeader);

        /// <summary>Persists chain of headers to the database.</summary>
        Task SaveAsync(ChainIndexer chainIndexer);
    }

    public class ChainRepository : IChainRepository
    {
        private readonly IChainStore chainStore;

        /// <summary>Instance logger.</summary>
        private BlockLocator locator;

        private readonly object lockObj;

        public Network Network { get; }

        public ChainRepository(ILoggerFactory loggerFactory, IChainStore chainStore, Network network)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));

            this.chainStore = chainStore;
            this.Network = network;
            this.lockObj = new object();
        }

        /// <inheritdoc />
        public Task<ChainedHeader> LoadAsync(ChainedHeader genesisHeader)
        {
            Task<ChainedHeader> task = Task.Run(() =>
            {
                lock (this.lockObj)
                {
                    ChainedHeader tip = null;

                    ChainData data = this.chainStore.GetChainData(0);

                    if (data == null)
                    {
                        genesisHeader.SetChainStore(this.chainStore);
                        return genesisHeader;
                    }

                    Guard.Assert(data.Hash == genesisHeader.HashBlock); // can't swap networks

                    int index = 0;
                    while (true)
                    {
                        data = this.chainStore.GetChainData(index);

                        if (data == null)
                            break;

                        tip = new ChainedHeader(data.Hash, data.Work, tip);
                        if (tip.Height == 0) tip.SetChainStore(this.chainStore);
                        index++;
                    }

                    if (tip == null)
                    {
                        genesisHeader.SetChainStore(this.chainStore);
                        tip = genesisHeader;
                    }

                    this.locator = tip.GetLocator();
                    return tip;
                }
            });

            return task;
        }

        /// <inheritdoc />
        public Task SaveAsync(ChainIndexer chainIndexer)
        {
            Guard.NotNull(chainIndexer, nameof(chainIndexer));

            Task task = Task.Run(() =>
            {
                lock (this.lockObj)
                {
                    ChainedHeader fork = this.locator == null ? null : chainIndexer.FindFork(this.locator);
                    ChainedHeader tip = chainIndexer.Tip;
                    ChainedHeader toSave = tip;

                    var headers = new List<ChainedHeader>();
                    while (toSave != fork)
                    {
                        headers.Add(toSave);
                        toSave = toSave.Previous;
                    }

                    var items = headers.OrderBy(b => b.Height).Select(h => new ChainDataItem
                    {
                        Height = h.Height,
                        Data = new ChainData { Hash = h.HashBlock, Work = h.ChainWorkBytes }
                    });

                    this.chainStore.PutChainData(items);

                    this.locator = tip.GetLocator();
                }
            });

            return task;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (this.chainStore as IDisposable)?.Dispose();
        }

        public class ChainRepositoryData : IBitcoinSerializable
        {
            public uint256 Hash;
            public byte[] Work;

            public ChainRepositoryData()
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
    }
}