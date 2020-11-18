﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Blockcore.Configuration;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks;
using Blockcore.Utilities;
using FluentAssertions;
using NBitcoin;
using Xunit;

namespace Blockcore.Tests.Common
{
    public class TestBase
    {
        public Network Network { get; protected set; }
        public DataStoreSerializer DataStoreSerializer { get; }

        /// <summary>
        /// Initializes logger factory for inherited tests.
        /// </summary>
        public TestBase(Network network)
        {
            this.Network = network;
            this.DataStoreSerializer = new DataStoreSerializer(network.Consensus.ConsensusFactory);
        }

        public static string AssureEmptyDir(string dir)
        {
            string uniqueDirName = $"{dir}-{DateTime.UtcNow:ddMMyyyyTHH.mm.ss.fff}";
            Directory.CreateDirectory(uniqueDirName);
            return uniqueDirName;
        }

        /// <summary>
        /// Creates a directory and initializes a <see cref="DataFolder"/> for a test, based on the name of the class containing the test and the name of the test.
        /// </summary>
        /// <param name="caller">The calling object, from which we derive the namespace in which the test is contained.</param>
        /// <param name="callingMethod">The name of the test being executed. A directory with the same name will be created.</param>
        /// <returns>The <see cref="DataFolder"/> that was initialized.</returns>
        public static DataFolder CreateDataFolder(object caller, [System.Runtime.CompilerServices.CallerMemberName] string callingMethod = "")
        {
            string directoryPath = GetTestDirectoryPath(caller, callingMethod);
            var dataFolder = new DataFolder(new NodeSettings(networksSelector: Networks.Bitcoin.Networks.Bitcoin, args: new string[] { $"-datadir={AssureEmptyDir(directoryPath)}" }).DataDir);
            return dataFolder;
        }

        /// <summary>
        /// Creates a directory for a test, based on the name of the class containing the test and the name of the test.
        /// </summary>
        /// <param name="caller">The calling object, from which we derive the namespace in which the test is contained.</param>
        /// <param name="callingMethod">The name of the test being executed. A directory with the same name will be created.</param>
        /// <returns>The path of the directory that was created.</returns>
        public static string CreateTestDir(object caller, [System.Runtime.CompilerServices.CallerMemberName] string callingMethod = "")
        {
            string directoryPath = GetTestDirectoryPath(caller, callingMethod);
            return AssureEmptyDir(directoryPath);
        }

        /// <summary>
        /// Creates a directory for a test.
        /// </summary>
        /// <param name="testDirectory">The directory in which the test files are contained.</param>
        /// <returns>The path of the directory that was created.</returns>
        public static string CreateTestDir(string testDirectory)
        {
            string directoryPath = GetTestDirectoryPath(testDirectory);
            return AssureEmptyDir(directoryPath);
        }

        /// <summary>
        /// Gets the path of the directory that <see cref="CreateTestDir(object, string)"/> or <see cref="CreateDataFolder(object, string)"/> would create.
        /// </summary>
        /// <remarks>The path of the directory is of the form TestCase/{testClass}/{testName}.</remarks>
        /// <param name="caller">The calling object, from which we derive the namespace in which the test is contained.</param>
        /// <param name="callingMethod">The name of the test being executed. A directory with the same name will be created.</param>
        /// <returns>The path of the directory.</returns>
        public static string GetTestDirectoryPath(object caller, [System.Runtime.CompilerServices.CallerMemberName] string callingMethod = "")
        {
            return GetTestDirectoryPath(Path.Combine(caller.GetType().Name, callingMethod));
        }

        /// <summary>
        /// Gets the path of the directory that <see cref="CreateTestDir(object, string)"/> or <see cref="CreateDataFolder(object, string)"/> would create.
        /// </summary>
        /// <remarks>The path of the directory is of the form TestCase/{testClass}/{testName}.</remarks>
        /// <param name="testDirectory">The directory in which the test files are contained.</param>
        /// <returns>The path of the directory.</returns>
        public static string GetTestDirectoryPath(string testDirectory)
        {
            return Path.Combine("..", "..", "..", "..", "..", "TestCase", testDirectory);
        }

        public void AppendBlocksToChain(ChainIndexer chainIndexer, IEnumerable<Block> blocks)
        {
            foreach (Block block in blocks)
            {
                if (chainIndexer.Tip != null)
                    block.Header.HashPrevBlock = chainIndexer.Tip.HashBlock;
                chainIndexer.SetTip(block.Header);
            }
        }

        public List<Block> CreateBlocks(int amount, bool bigBlocks = false)
        {
            var blocks = new List<Block>();
            for (int i = 0; i < amount; i++)
            {
                Block block = this.CreateBlock(i);
                block.Header.HashPrevBlock = blocks.LastOrDefault()?.GetHash() ?? this.Network.GenesisHash;
                blocks.Add(block);
            }

            return blocks;
        }

        private Block CreateBlock(int blockNumber, bool bigBlocks = false)
        {
            Block block = this.Network.CreateBlock();

            int transactionCount = bigBlocks ? 1000 : 10;

            for (int j = 0; j < transactionCount; j++)
            {
                Transaction trx = this.Network.CreateTransaction();

                // Coinbase
                block.AddTransaction(this.Network.CreateTransaction());

                trx.AddInput(new TxIn(Script.Empty));
                trx.AddOutput(Money.COIN + j + blockNumber, new Script(Enumerable.Range(1, 5).SelectMany(index => Guid.NewGuid().ToByteArray())));

                trx.AddInput(new TxIn(Script.Empty));
                trx.AddOutput(Money.COIN + j + blockNumber + 1, new Script(Enumerable.Range(1, 5).SelectMany(index => Guid.NewGuid().ToByteArray())));

                block.AddTransaction(trx);
            }

            block.UpdateMerkleRoot();

            return block;
        }

        public ProvenBlockHeader CreateNewProvenBlockHeaderMock(PosBlock posBlock = null)
        {
            PosBlock block = posBlock == null ? this.CreatePosBlock() : posBlock;
            ProvenBlockHeader provenBlockHeader = ((PosConsensusFactory)this.Network.Consensus.ConsensusFactory).CreateProvenBlockHeader(block);

            return provenBlockHeader;
        }

        /// <summary>
        /// Creates a list of Proof of Stake blocks.
        /// </summary>
        /// <param name="amount">The amount of blocks to create.</param>
        public List<Block> CreatePosBlocks(int amount)
        {
            var blocks = new List<Block>();
            for (int i = 0; i < amount; i++)
            {
                PosBlock block = this.CreatePosBlock();
                block.Header.HashPrevBlock = blocks.LastOrDefault()?.GetHash() ?? this.Network.GenesisHash;
                blocks.Add(block);
            }

            return blocks;
        }

        public PosBlock CreatePosBlock()
        {
            // Create coinstake Tx.
            Transaction previousTx = this.Network.CreateTransaction();
            previousTx.AddOutput(new TxOut());

            Transaction coinstakeTx = this.Network.CreateTransaction();
            coinstakeTx.AddOutput(new TxOut(0, Script.Empty));
            coinstakeTx.AddOutput(new TxOut(50, new Script()));
            coinstakeTx.AddInput(previousTx, 0);
            coinstakeTx.IsCoinStake.Should().BeTrue();
            coinstakeTx.IsCoinBase.Should().BeFalse();

            // Create coinbase Tx.
            Transaction coinBaseTx = this.Network.CreateTransaction();
            coinBaseTx.AddOutput(100, new Script());
            coinBaseTx.AddInput(new TxIn());
            coinBaseTx.IsCoinBase.Should().BeTrue();
            coinBaseTx.IsCoinStake.Should().BeFalse();

            var block = (PosBlock)this.Network.CreateBlock();
            block.AddTransaction(coinBaseTx);
            block.AddTransaction(coinstakeTx);
            block.BlockSignature = new BlockSignature { Signature = new byte[] { 0x2, 0x3 } };

            return block;
        }

        /// <summary>
        /// Builds a chain of proven headers.
        /// </summary>
        /// <param name="blockCount">The amount of blocks to chain.</param>
        /// <param name="startingHeader">Build the chain from this header, if not start from genesis.</param>
        /// <returns>Tip of a created chain of headers.</returns>
        public ChainedHeader BuildProvenHeaderChain(int blockCount, ChainedHeader startingHeader = null)
        {
            startingHeader = startingHeader ?? ChainedHeadersHelper.CreateGenesisChainedHeader(this.Network);

            for (int i = 1; i < blockCount; i++)
            {
                PosBlock block = this.CreatePosBlock();
                ProvenBlockHeader header = ((PosConsensusFactory)this.Network.Consensus.ConsensusFactory).CreateProvenBlockHeader(block);

                header.PosBlockHeader.Nonce = RandomUtils.GetUInt32();
                header.PosBlockHeader.HashPrevBlock = startingHeader.HashBlock;
                header.PosBlockHeader.Bits = Target.Difficulty1;

                ChainedHeader prevHeader = startingHeader;
                startingHeader = new ChainedHeader(header.PosBlockHeader, header.GetHash(), prevHeader.Height + 1);

                startingHeader.SetPrivatePropertyValue("Previous", prevHeader);
                prevHeader.Next.Add(startingHeader);
            }

            return startingHeader;
        }

        public ChainedHeader BuildProvenHeaderChainFromBlocks(List<Block> posBlocks)
        {
            ChainedHeader currentHeader = ChainedHeadersHelper.CreateGenesisChainedHeader(this.Network);

            foreach (PosBlock posBlock in posBlocks)
            {
                ProvenBlockHeader header = ((PosConsensusFactory)this.Network.Consensus.ConsensusFactory).CreateProvenBlockHeader(posBlock);

                header.PosBlockHeader.Nonce = RandomUtils.GetUInt32();
                header.PosBlockHeader.HashPrevBlock = currentHeader.HashBlock;
                header.PosBlockHeader.Bits = Target.Difficulty1;

                ChainedHeader prevHeader = currentHeader;
                currentHeader = new ChainedHeader(header, header.GetHash(), prevHeader);

                prevHeader.Next.Add(currentHeader);
            }

            return currentHeader;
        }

        public void CompareCollections(List<ChainedHeader> chainedHeaders, SortedDictionary<int, ProvenBlockHeader> sortedDictionary)
        {
            Assert.Equal(chainedHeaders.Count, sortedDictionary.Count);

            for (int i = 0; i < chainedHeaders.Count; i++)
            {
                Assert.Equal(chainedHeaders[i].HashBlock, sortedDictionary.ElementAt(i).Value.GetHash());
                if (i > 0)
                {
                    Assert.Equal(sortedDictionary.ElementAt(i - 1).Value.GetHash(), sortedDictionary.ElementAt(i).Value.HashPrevBlock);
                }
            }
        }

        public SortedDictionary<int, ProvenBlockHeader> ConvertToDictionaryOfProvenHeaders(ChainedHeader tip)
        {
            var headers = new SortedDictionary<int, ProvenBlockHeader>();

            ChainedHeader currentHeader = tip;

            while (currentHeader.Height != 0)
            {
                headers.Add(currentHeader.Height, (currentHeader.Header as PosBlockHeader).ProvenBlockHeader);

                currentHeader = currentHeader.Previous;
            }

            return headers;
        }

        /// <summary>
        /// Waits for the given action to complete.
        /// </summary>
        /// <param name="act">The function to run (it must return true or false).</param>
        /// <param name="failureReason">The reason the function failed to complete.</param>
        /// <param name="waitTimeSeconds">The amount of time the function is allowed to run.</param>
        /// <param name="retryDelayInMiliseconds">The delay before execution of the function is tried again.</param>
        /// <param name="cancellationToken">Used in the event that a custom cancellation token is required.</param>
        public static void WaitLoop(Func<bool> act, string failureReason = "Unknown Reason", int waitTimeSeconds = 60, int retryDelayInMiliseconds = 1000, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken == default(CancellationToken))
            {
                cancellationToken = new CancellationTokenSource(Debugger.IsAttached ? 15 * 60 * 1000 : waitTimeSeconds * 1000).Token;
            }

            while (!act())
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(retryDelayInMiliseconds);
                }
                catch (OperationCanceledException e)
                {
                    Assert.False(true, $"{failureReason}{Environment.NewLine}{e.Message} [{e.InnerException?.Message}]");
                }
            }
        }

        public static void WaitLoopMessage(Func<(bool success, string message)> act, int waitTimeSeconds = 60)
        {
            CancellationToken cancellationToken = new CancellationTokenSource(Debugger.IsAttached ? 15 * 60 * 1000 : waitTimeSeconds * 1000).Token;

            (bool success, string message) = act();

            while (!success)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);

                    (success, message) = act();
                }
                catch (OperationCanceledException e)
                {
                    Assert.False(true, $"{message}{Environment.NewLine}{e.Message} [{e.InnerException?.Message}]");
                }
            }
        }
    }
}