using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Features.Base.Persistence.LevelDb;
using Blockcore.Tests.Common;
using Blockcore.Utilities;
using LevelDB;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Xunit;

namespace Blockcore.Tests.Base
{
    public class ChainRepositoryTest : TestBase
    {
        private readonly DataStoreSerializer dataStoreSerializer;

        public ChainRepositoryTest() : base(KnownNetworks.StratisRegTest)
        {
            this.dataStoreSerializer = new DataStoreSerializer(this.Network.Consensus.ConsensusFactory);
        }

        [Fact]
        public void SaveChainToDisk()
        {
            string dir = CreateTestDir(this);
            var chain = new ChainIndexer(KnownNetworks.StratisRegTest);
            this.AppendBlock(chain);

            using (var repo = new ChainRepository(new LoggerFactory(), new LevelDbChainStore(chain.Network, new DataFolder(dir), chain), chain.Network))
            {
                repo.SaveAsync(chain).GetAwaiter().GetResult();
            }

            using (var engine = new DB(new Options { CreateIfMissing = true }, new DataFolder(dir).ChainPath))
            {
                ChainedHeader tip = null;
                var itr = engine.GetEnumerator();

                while (itr.MoveNext())
                {
                    if (itr.Current.Key[0] == 1)
                    {
                        var data = new ChainRepository.ChainRepositoryData();
                        data.FromBytes(itr.Current.Value.ToArray(), this.Network.Consensus.ConsensusFactory);

                        tip = new ChainedHeader(data.Hash, data.Work, tip);
                        if (tip.Height == 0) tip.SetChainStore(new ChainStore());
                    }
                }
                Assert.Equal(tip, chain.Tip);
            }
        }

        [Fact]
        public void LoadChainFromDisk()
        {
            string dir = CreateTestDir(this);
            var chain = new ChainIndexer(KnownNetworks.StratisRegTest);
            ChainedHeader tip = this.AppendBlock(chain);

            using (var engine = new DB(new Options { CreateIfMissing = true }, new DataFolder(dir).ChainPath))
            {
                using (var batch = new WriteBatch())
                {
                    ChainedHeader toSave = tip;
                    var blocks = new List<ChainedHeader>();
                    while (toSave != null)
                    {
                        blocks.Insert(0, toSave);
                        toSave = toSave.Previous;
                    }

                    foreach (ChainedHeader block in blocks)
                    {
                        batch.Put(DBH.Key(1, BitConverter.GetBytes(block.Height)),
                            new ChainRepository.ChainRepositoryData()
                            { Hash = block.HashBlock, Work = block.ChainWorkBytes }
                                .ToBytes(this.Network.Consensus.ConsensusFactory));
                    }

                    engine.Write(batch);
                }
            }
            using (var repo = new ChainRepository(new LoggerFactory(), new LevelDbChainStore(chain.Network, new DataFolder(dir), chain), chain.Network))
            {
                var testChain = new ChainIndexer(KnownNetworks.StratisRegTest);
                testChain.SetTip(repo.LoadAsync(testChain.Genesis).GetAwaiter().GetResult());
                Assert.Equal(tip, testChain.Tip);
            }
        }

        public ChainedHeader AppendBlock(ChainedHeader previous, params ChainIndexer[] chainsIndexer)
        {
            ChainedHeader last = null;
            uint nonce = RandomUtils.GetUInt32();
            foreach (ChainIndexer chain in chainsIndexer)
            {
                Block block = this.Network.Consensus.ConsensusFactory.CreateBlock();
                block.AddTransaction(this.Network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.HashPrevBlock = previous == null ? chain.Tip.HashBlock : previous.HashBlock;
                block.Header.Nonce = nonce;
                if (!chain.TrySetTip(block.Header, out last))
                    throw new InvalidOperationException("Previous not existing");
            }
            return last;
        }

        private ChainedHeader AppendBlock(params ChainIndexer[] chainsIndexer)
        {
            ChainedHeader index = null;
            return this.AppendBlock(index, chainsIndexer);
        }
    }
}