using System;
using System.IO;
using System.Net.Http;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Tests.Common;
using Blockcore.Utilities.Extensions;
using Xunit;

namespace NBitcoin.Tests
{
    public class pow_tests
    {
        [Fact]
        public void CanCalculatePowCorrectly()
        {
            var chain = new ChainIndexer(KnownNetworks.Main);
            EnsureDownloaded("MainChain.dat", "https://aois.blob.core.windows.net/public/MainChain.dat");
            chain.Load(File.ReadAllBytes("MainChain.dat"));
            foreach (ChainedHeader block in chain.EnumerateAfter(chain.Genesis))
            {
                Target thisWork = block.GetWorkRequired(KnownNetworks.Main.Consensus);
                Target thisWork2 = this.GetNextWorkRequired(KnownNetworks.Main.Consensus, block.Previous);
                Assert.Equal(thisWork, thisWork2);
                Assert.True(this.CheckProofOfWorkAndTarget(KnownNetworks.Main.Consensus, block));
            }
        }

        /// <summary>
        /// Verify proof of work of the header of this chain using consensus.
        /// </summary>
        /// <param name="consensus">Consensus rules to use for this validation.</param>
        /// <returns>Whether proof of work is valid.</returns>
        public bool CheckProofOfWorkAndTarget(IConsensus consensus, ChainedHeader header)
        {
            return (header.Height == 0) || (header.Header.CheckProofOfWork() && (header.Header.Bits == header.GetWorkRequired(consensus)));
        }

        private static void EnsureDownloaded(string file, string url)
        {
            if (File.Exists(file))
                return;
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            byte[] data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
            File.WriteAllBytes(file, data);
        }

        /// <summary>
        /// Gets the proof of work target for a potential new block after this entry on the chain.
        /// </summary>
        /// <param name="consensus">Consensus rules to use for this computation.</param>
        /// <returns>The target proof of work.</returns>
        public Target GetNextWorkRequired(IConsensus consensus, ChainedHeader chainedHeader)
        {
            BlockHeader dummy = consensus.ConsensusFactory.CreateBlockHeader();
            dummy.HashPrevBlock = chainedHeader.HashBlock;
            dummy.BlockTime = DateTimeOffset.UtcNow;

            return new ChainedHeader(dummy, dummy.GetHash(), chainedHeader).GetWorkRequired(consensus);
        }
    }
}