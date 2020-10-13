using System;
using System.Collections.Generic;
using System.IO;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using NBitcoin;
using Xunit;

namespace Blockcore.Tests.Common
{
    public static class ConsensusChainIndexerExtensions
    {
        /// <summary>
        /// Sets the tip of this chain based upon another block header.
        /// </summary>
        /// <param name="header">The block header to set to tip.</param>
        /// <returns>Whether the tip was set successfully.</returns>
        public static bool SetTip(this ChainIndexer chainIndexer, BlockHeader header)
        {
            ChainedHeader chainedHeader;
            return chainIndexer.TrySetTip(header, out chainedHeader);
        }

        public static bool TrySetTip(this ChainIndexer chainIndexer, BlockHeader header, out ChainedHeader chainedHeader)
        {
            if (header == null)
                throw new ArgumentNullException("header");

            chainedHeader = null;
            ChainedHeader prev = chainIndexer.GetHeader(header.HashPrevBlock);
            if (prev == null)
                return false;

            chainedHeader = new ChainedHeader(header, header.GetHash(), chainIndexer.GetHeader(header.HashPrevBlock));
            chainIndexer.SetTip(chainedHeader);
            return true;
        }

        public static ChainedHeader SetTip(this ChainIndexer chainIndexer, ChainedHeader block)
        {
            ChainedHeader fork = chainIndexer.Tip.FindFork(block);

            chainIndexer.Initialize(block);

            return fork;
        }

        private static IEnumerable<ChainedHeader> EnumerateThisToFork(this ChainIndexer chainIndexer, ChainedHeader block)
        {
            if (chainIndexer.Tip == null)
                yield break;

            ChainedHeader tip = chainIndexer.Tip;
            while (true)
            {
                if (ReferenceEquals(null, block) || ReferenceEquals(null, tip))
                    throw new InvalidOperationException("No fork found between the two chains");

                if (tip.Height > block.Height)
                {
                    yield return tip;
                    tip = tip.Previous;
                }
                else if (tip.Height < block.Height)
                {
                    block = block.Previous;
                }
                else if (tip.Height == block.Height)
                {
                    if (tip.HashBlock == block.HashBlock)
                        break;

                    yield return tip;

                    block = block.Previous;
                    tip = tip.Previous;
                }
            }
        }

        public static ChainIndexer Load(this ChainIndexer chainIndexer, byte[] chain)
        {
            using (var ms = new MemoryStream(chain))
            {
                var bitcoinStream = new BitcoinStream(ms, false, chainIndexer.Network.Consensus.ConsensusFactory);

                try
                {
                    int height = 0;
                    while (true)
                    {
                        uint256.MutableUint256 id = null;
                        bitcoinStream.ReadWrite<uint256.MutableUint256>(ref id);
                        BlockHeader header = null;
                        bitcoinStream.ReadWrite(ref header);
                        if (height == 0)
                        {
                            Assert.True(header.GetHash() == chainIndexer.Tip.HashBlock);
                        }
                        else if (chainIndexer.Tip.HashBlock == header.HashPrevBlock && !(header.IsNull && header.Nonce == 0))
                        {
                            chainIndexer.Add(new ChainedHeader(header, id.Value, chainIndexer.Tip));
                        }
                        else
                            break;

                        height++;
                    }
                }
                catch (EndOfStreamException)
                {
                }

                return chainIndexer;
            }
        }

        public static byte[] ToBytes(this ChainIndexer chainIndexer)
        {
            using (var ms = new MemoryStream())
            {
                var stream = new BitcoinStream(ms, true, chainIndexer.Network.Consensus.ConsensusFactory);

                for (int i = 0; i < chainIndexer.Tip.Height + 1; i++)
                {
                    ChainedHeader block = chainIndexer.GetHeader(i);
                    stream.ReadWrite(block.HashBlock.AsBitcoinSerializable());
                    stream.ReadWrite(block.Header);
                }

                return ms.ToArray();
            }
        }
    }
}