using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Features.Consensus.Persistence.LevelDb;
using Blockcore.Features.Consensus.ProvenBlockHeaders;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Tests.Common;
using Blockcore.Tests.Common.Logging;
using Blockcore.Utilities;
using DBreeze;
using DBreeze.Utils;
using FluentAssertions;
using LevelDB;
using Microsoft.Extensions.Logging;
using Moq;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Consensus.Tests.ProvenBlockHeaders
{
    public class ProvenBlockHeaderRepositoryTests : LogsTestBase
    {
        private readonly Mock<ILoggerFactory> loggerFactory;
        private readonly DataStoreSerializer dataStoreSerializer;
        private static readonly byte ProvenBlockHeaderTable = 1;
        private static readonly byte BlockHashHeightTable = 2;

        public ProvenBlockHeaderRepositoryTests() : base(KnownNetworks.StratisTest)
        {
            this.loggerFactory = new Mock<ILoggerFactory>();
            this.dataStoreSerializer = new DataStoreSerializer(this.Network.Consensus.ConsensusFactory);
        }

        [Fact]
        public void Initializes_Genesis_ProvenBlockHeader_OnLoadAsync()
        {
            string folder = CreateTestDir(this);

            // Initialise the repository - this will set-up the genesis blockHash (blockId).
            using (IProvenBlockHeaderRepository repository = this.SetupRepository(this.Network, folder))
            {
                // Check the BlockHash (blockId) exists.
                repository.TipHashHeight.Height.Should().Be(0);
                repository.TipHashHeight.Hash.Should().Be(this.Network.GetGenesis().GetHash());
            }
        }

        [Fact]
        public async Task PutAsync_WritesProvenBlockHeaderAndSavesBlockHashAsync()
        {
            string folder = CreateTestDir(this);

            ProvenBlockHeader provenBlockHeaderIn = CreateNewProvenBlockHeaderMock();

            var blockHashHeightPair = new HashHeightPair(provenBlockHeaderIn.GetHash(), 0);
            var items = new SortedDictionary<int, ProvenBlockHeader>() { { 0, provenBlockHeaderIn } };

            using (IProvenBlockHeaderRepository repo = this.SetupRepository(this.Network, folder))
            {
                await repo.PutAsync(items, blockHashHeightPair);
            }

            using (var engine = new DB(new Options() { CreateIfMissing = true }, folder))
            {
                var headerOut = this.dataStoreSerializer.Deserialize<ProvenBlockHeader>(engine.Get(DBH.Key(ProvenBlockHeaderTable, BitConverter.GetBytes(blockHashHeightPair.Height))));
                var hashHeightPairOut = this.DataStoreSerializer.Deserialize<HashHeightPair>(engine.Get(DBH.Key(BlockHashHeightTable, new byte[] { 1 })));

                headerOut.Should().NotBeNull();
                headerOut.GetHash().Should().Be(provenBlockHeaderIn.GetHash());

                hashHeightPairOut.Should().NotBeNull();
                hashHeightPairOut.Hash.Should().Be(provenBlockHeaderIn.GetHash());
            }
        }

        [Fact]
        public async Task PutAsync_Inserts_MultipleProvenBlockHeadersAsync()
        {
            string folder = CreateTestDir(this);

            PosBlock posBlock = CreatePosBlock();
            ProvenBlockHeader header1 = CreateNewProvenBlockHeaderMock(posBlock);
            ProvenBlockHeader header2 = CreateNewProvenBlockHeaderMock(posBlock);

            var items = new SortedDictionary<int, ProvenBlockHeader>() { { 0, header1 }, { 1, header2 } };

            // Put the items in the repository.
            using (IProvenBlockHeaderRepository repo = this.SetupRepository(this.Network, folder))
            {
                await repo.PutAsync(items, new HashHeightPair(header2.GetHash(), items.Count - 1));
            }

            // Check the ProvenBlockHeader exists in the database.
            using (var engine = new DB(new Options() { CreateIfMissing = true }, folder))
            {
                var headersOut = new Dictionary<byte[], byte[]>();
                var enumeator = engine.GetEnumerator();
                while (enumeator.MoveNext())
                    if (enumeator.Current.Key[0] == ProvenBlockHeaderTable)
                        headersOut.Add(enumeator.Current.Key, enumeator.Current.Value);

                headersOut.Keys.Count.Should().Be(2);
                this.dataStoreSerializer.Deserialize<ProvenBlockHeader>(headersOut.First().Value).GetHash().Should().Be(items[0].GetHash());
                this.dataStoreSerializer.Deserialize<ProvenBlockHeader>(headersOut.Last().Value).GetHash().Should().Be(items[1].GetHash());
            }
        }

        [Fact]
        public async Task GetAsync_ReadsProvenBlockHeaderAsync()
        {
            string folder = CreateTestDir(this);

            ProvenBlockHeader headerIn = CreateNewProvenBlockHeaderMock();

            int blockHeight = 1;

            using (var engine = new DB(new Options() { CreateIfMissing = true }, folder))
            {
                engine.Put(DBH.Key(ProvenBlockHeaderTable, BitConverter.GetBytes(blockHeight)), this.dataStoreSerializer.Serialize(headerIn));
            }

            // Query the repository for the item that was inserted in the above code.
            using (LevelDbProvenBlockHeaderRepository repo = this.SetupRepository(this.Network, folder))
            {
                var headerOut = await repo.GetAsync(blockHeight).ConfigureAwait(false);

                headerOut.Should().NotBeNull();
                uint256.Parse(headerOut.ToString()).Should().Be(headerOut.GetHash());
            }
        }

        [Fact]
        public async Task GetAsync_WithWrongBlockHeightReturnsNullAsync()
        {
            string folder = CreateTestDir(this);

            using (var engine = new DB(new Options() { CreateIfMissing = true }, folder))
            {
                engine.Put(DBH.Key(ProvenBlockHeaderTable, BitConverter.GetBytes(1)), this.dataStoreSerializer.Serialize(CreateNewProvenBlockHeaderMock()));
                engine.Put(DBH.Key(BlockHashHeightTable, new byte[0]), this.DataStoreSerializer.Serialize(new HashHeightPair(new uint256(), 1)));
            }

            using (LevelDbProvenBlockHeaderRepository repo = this.SetupRepository(this.Network, folder))
            {
                // Select a different block height.
                ProvenBlockHeader outHeader = await repo.GetAsync(2).ConfigureAwait(false);
                outHeader.Should().BeNull();

                // Select the original item inserted into the table
                outHeader = await repo.GetAsync(1).ConfigureAwait(false);
                outHeader.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task PutAsyncsDisposeOnInitialiseShouldBeAtLastSavedTipAsync()
        {
            string folder = CreateTestDir(this);

            PosBlock posBlock = CreatePosBlock();
            var headers = new SortedDictionary<int, ProvenBlockHeader>();

            for (int i = 0; i < 10; i++)
            {
                headers.Add(i, CreateNewProvenBlockHeaderMock(posBlock));
            }

            // Put the items in the repository.
            using (IProvenBlockHeaderRepository repo = this.SetupRepository(this.Network, folder))
            {
                await repo.PutAsync(headers, new HashHeightPair(headers.Last().Value.GetHash(), headers.Count - 1));
            }

            using (IProvenBlockHeaderRepository newRepo = this.SetupRepository(this.Network, folder))
            {
                newRepo.TipHashHeight.Hash.Should().Be(headers.Last().Value.GetHash());
                newRepo.TipHashHeight.Height.Should().Be(headers.Count - 1);
            }
        }

        private LevelDbProvenBlockHeaderRepository SetupRepository(Network network, string folder)
        {
            var repo = new LevelDbProvenBlockHeaderRepository(network, folder, this.LoggerFactory.Object, this.dataStoreSerializer);

            Task task = repo.InitializeAsync();

            task.Wait();

            return repo;
        }
    }
}