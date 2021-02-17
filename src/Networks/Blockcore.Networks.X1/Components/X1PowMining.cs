using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.Miner;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Interfaces;
using Blockcore.Mining;
using Blockcore.Networks;
using Blockcore.Networks.X1.Consensus;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;

namespace Blockcore.Networks.X1.Components
{
    public class X1PowMining : IPowMining
    {
        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncProvider asyncProvider;

        /// <summary>Builder that creates a proof-of-work block template.</summary>
        private readonly IBlockProvider blockProvider;

        /// <summary>Thread safe chain of block headers from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>Manager of the longest fully validated chain of blocks.</summary>
        private readonly IConsensusManager consensusManager;

        /// <summary>Provider of time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        private uint256 hashPrevBlock;

        private const int InnerLoopCount = 0x10000;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Factory for creating loggers.</summary>
        private readonly ILoggerFactory loggerFactory;

        private readonly IInitialBlockDownloadState initialBlockDownloadState;

        /// <summary>Transaction memory pool for managing transactions in the memory pool.</summary>
        private readonly ITxMempool mempool;

        /// <summary>A lock for managing asynchronous access to memory pool.</summary>
        private readonly MempoolSchedulerLock mempoolLock;

        /// <summary>The async loop we need to wait upon before we can shut down this feature.</summary>
        private IAsyncLoop miningLoop;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        private readonly Network network;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly INodeLifetime nodeLifetime;

        /// <summary>SpartaCrypt OpenCL Miner.</summary>
        private readonly OpenCLMiner openCLMiner;

        /// <summary>SpartaCrypt OpenCL Miner.</summary>
        private readonly X1MinerSettings minerSettings;

        /// <summary>Constant for hash rate calculation.</summary>
        readonly BigInteger pow256 = BigInteger.ValueOf(2).Pow(256);

        /// <summary>Stopwatch for hash rate calculation.</summary>
        readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// A cancellation token source that can cancel the mining processes and is linked to the <see cref="INodeLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource miningCancellationTokenSource;

        public X1PowMining(
            IAsyncProvider asyncProvider,
            IBlockProvider blockProvider,
            IConsensusManager consensusManager,
            ChainIndexer chainIndexer,
            IDateTimeProvider dateTimeProvider,
            ITxMempool mempool,
            MempoolSchedulerLock mempoolLock,
            Network network,
            INodeLifetime nodeLifetime,
            ILoggerFactory loggerFactory,
            IInitialBlockDownloadState initialBlockDownloadState,
            MinerSettings minerSettings)
        {
            this.asyncProvider = asyncProvider;
            this.blockProvider = blockProvider;
            this.chainIndexer = chainIndexer;
            this.consensusManager = consensusManager;
            this.dateTimeProvider = dateTimeProvider;
            this.loggerFactory = loggerFactory;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.mempool = mempool;
            this.mempoolLock = mempoolLock;
            this.network = network;
            this.nodeLifetime = nodeLifetime;
            this.miningCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { this.nodeLifetime.ApplicationStopping });
            this.minerSettings = (X1MinerSettings)minerSettings;
            if (this.minerSettings.UseOpenCL)
            {
                this.openCLMiner = new OpenCLMiner(this.minerSettings, loggerFactory);
            }
        }

        /// <inheritdoc/>
        public void Mine(Script reserveScript)
        {
            if (this.miningLoop != null)
                return;

            this.miningCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { this.nodeLifetime.ApplicationStopping });

            this.miningLoop = this.asyncProvider.CreateAndRunAsyncLoop("PowMining.Mine", token =>
            {
                try
                {
                    this.GenerateBlocks(new ReserveScript { ReserveFullNodeScript = reserveScript }, int.MaxValue, int.MaxValue);
                }
                catch (OperationCanceledException)
                {
                    // Application stopping, nothing to do as the loop will be stopped.
                }
                catch (MinerException me)
                {
                    this.logger.LogWarning($"{nameof(MinerException)} in mining loop: {me}");
                }
                catch (ConsensusErrorException cee)
                {
                    this.logger.LogWarning($"{nameof(ConsensusErrorException)} in mining loop: {cee}");
                }
                catch (ConsensusException ce)
                {
                    this.logger.LogWarning($"{nameof(ConsensusException)} in mining loop: {ce}");
                }
                catch (Exception e)
                {
                    this.logger.LogError($"{e.GetType()} in mining loop, exiting mining loop: {e}");
                    throw;
                }

                return Task.CompletedTask;
            },
            this.miningCancellationTokenSource.Token,
            repeatEvery: TimeSpans.Second,
            startAfter: TimeSpans.TenSeconds);
        }

        /// <inheritdoc/>
        public void StopMining()
        {
            this.miningCancellationTokenSource.Cancel();
            this.miningLoop?.Dispose();
            this.miningLoop = null;
            this.miningCancellationTokenSource.Dispose();
            this.miningCancellationTokenSource = null;
        }

        /// <inheritdoc/>
        public List<uint256> GenerateBlocks(ReserveScript reserveScript, ulong amountOfBlocksToMine, ulong maxTries)
        {
            var context = new MineBlockContext(amountOfBlocksToMine, (ulong)this.chainIndexer.Height, maxTries, reserveScript);

            while (context.MiningCanContinue)
            {
                if (!this.ConsensusIsAtTip(context))
                    continue;

                if (!this.BuildBlock(context))
                    continue;

                if (!this.IsProofOfWorkAllowed(context))
                    continue;

                if (!this.MineBlock(context))
                    break;

                if (!this.ValidateMinedBlock(context))
                    continue;

                if (!this.ValidateAndConnectBlock(context))
                    continue;

                this.OnBlockMined(context);
            }

            return context.Blocks;
        }

        private bool MineBlock(MineBlockContext context)
        {
            if (this.network.NetworkType == NetworkType.Regtest)
                return MineBlockRegTest(context);

            if (this.minerSettings.UseOpenCL && this.openCLMiner.CanMine())
                return MineBlockOpenCL(context);

            return MineBlockCpu(context);
        }

        private bool MineBlockCpu(MineBlockContext context)
        {
            context.ExtraNonce = IncrementExtraNonce(context.BlockTemplate.Block, context.ChainTip, context.ExtraNonce);

            Block block = context.BlockTemplate.Block;
            block.Header.Nonce = 0;

            uint loopLength = 2_000_000;
            int threads = Math.Max(1, Math.Min(this.minerSettings.MineThreadCount, Environment.ProcessorCount));

            int batch = threads;
            var totalNonce = batch * loopLength;
            uint winnerNonce = 0;
            bool found = false;

            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = threads, CancellationToken = this.miningCancellationTokenSource.Token };


            this.stopwatch.Restart();

            int fromInclusive = context.ExtraNonce * batch;
            int toExclusive = fromInclusive + batch;

            Parallel.For(fromInclusive, toExclusive, options, (index, state) =>
            {
                if (this.miningCancellationTokenSource.Token.IsCancellationRequested)
                    return;

                uint256 bits = block.Header.Bits.ToUInt256();

                var headerBytes = block.Header.ToBytes(this.network.Consensus.ConsensusFactory);
                uint nonce = (uint)index * loopLength;

                var end = nonce + loopLength;

                while (nonce < end)
                {
                    if (CheckProofOfWork(headerBytes, nonce, bits))
                    {
                        winnerNonce = nonce;
                        found = true;
                        state.Stop();

                        return;
                    }

                    if (state.IsStopped)
                        return;

                    ++nonce;
                }
            });

            if (found)
            {
                block.Header.Nonce = winnerNonce;
                if (block.Header.CheckProofOfWork())
                    return true;
            }

            this.LogMiningInformation(context.ExtraNonce, totalNonce, this.stopwatch.Elapsed.TotalSeconds, block.Header.Bits.Difficulty, $"{threads} threads");

            return false;
        }

        private bool MineBlockOpenCL(MineBlockContext context)
        {
            Block block = context.BlockTemplate.Block;
            block.Header.Nonce = 0;
            context.ExtraNonce = this.IncrementExtraNonce(block, context.ChainTip, context.ExtraNonce);

            var iterations = uint.MaxValue / (uint)this.minerSettings.OpenCLWorksizeSplit;
            var nonceStart = ((uint)context.ExtraNonce - 1) * iterations;


            this.stopwatch.Restart();

            var headerBytes = block.Header.ToBytes(this.network.Consensus.ConsensusFactory);
            uint256 bits = block.Header.Bits.ToUInt256();
            var foundNonce = this.openCLMiner.FindPow(headerBytes, bits.ToBytes(), nonceStart, iterations);


            if (foundNonce > 0)
            {
                block.Header.Nonce = foundNonce;
                if (block.Header.CheckProofOfWork())
                {
                    return true;
                }
            }

            this.LogMiningInformation(context.ExtraNonce, iterations, this.stopwatch.Elapsed.TotalSeconds, block.Header.Bits.Difficulty, $"{this.openCLMiner.GetDeviceName()}");

            if (context.ExtraNonce >= this.minerSettings.OpenCLWorksizeSplit)
            {
                block.Header.Time += 1;
                context.ExtraNonce = 0;
            }

            return false;
        }

        private void LogMiningInformation(int extraNonce, long totalHashes, double totalSeconds, double difficultly, string minerInfo)
        {
            var MHashedPerSec = Math.Round((totalHashes / totalSeconds) / 1_000_000, 4);

            var currentDifficulty = BigInteger.ValueOf((long)difficultly);
            var MHashedPerSecTotal = currentDifficulty.Multiply(this.pow256)
                                         .Divide(Target.Difficulty1.ToBigInteger()).Divide(BigInteger.ValueOf(10 * 60))
                                         .LongValue / 1_000_000.0;

            this.logger.LogInformation($"Difficulty={difficultly}, extraNonce={extraNonce}, " +
                                       $"hashes={totalHashes}, execution={totalSeconds} sec, " +
                                       $"hash-rate={MHashedPerSec} MHash/sec ({minerInfo}), " +
                                       $"network hash-rate ~{MHashedPerSecTotal} MHash/sec");
        }

        private static bool CheckProofOfWork(byte[] header, uint nonce, uint256 bits)
        {
            var bytes = BitConverter.GetBytes(nonce);
            header[76] = bytes[0];
            header[77] = bytes[1];
            header[78] = bytes[2];
            header[79] = bytes[3];

            uint256 headerHash = Sha512T.GetHash(header);

            var res = headerHash <= bits;

            return res;
        }

        private bool IsProofOfWorkAllowed(MineBlockContext context)
        {
            var newBlockHeight = context.ChainTip.Height + 1;

            if (this.network.Consensus.Options is X1ConsensusOptions options)
            {
                if (options.IsAlgorithmAllowed(false, newBlockHeight))
                    return true;

                Task.Delay(1000).Wait(); // pause the miner
                return false;
            }

            return true;
        }


        /// <summary>
        /// Ensures that the node is synced before mining is allowed to start.
        /// </summary>
        private bool ConsensusIsAtTip(MineBlockContext context)
        {
            this.miningCancellationTokenSource.Token.ThrowIfCancellationRequested();

            context.ChainTip = this.consensusManager.Tip;

            // Genesis on a regtest network is a special case. We need to regard ourselves as outside of IBD to
            // bootstrap the mining.
            if (context.ChainTip.Height == 0)
                return true;

            if (this.initialBlockDownloadState.IsInitialBlockDownload())
            {
                Task.Delay(TimeSpan.FromMinutes(1), this.nodeLifetime.ApplicationStopping).GetAwaiter().GetResult();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates a proof of work or proof of stake block depending on the network the node is running on.
        /// <para>
        /// If the node is on a POS network, make sure the POS consensus rules are valid. This is required for
        /// generation of blocks inside tests, where it is possible to generate multiple blocks within one second.
        /// </para>
        /// </summary>
        private bool BuildBlock(MineBlockContext context)
        {
            context.BlockTemplate = this.blockProvider.BuildPowBlock(context.ChainTip, context.ReserveScript.ReserveFullNodeScript);

            if (this.network.Consensus.IsProofOfStake)
            {
                if (context.BlockTemplate.Block.Header.Time <= context.ChainTip.Header.Time)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Executes until the required work (difficulty) has been reached. This is the "mining" process.
        /// </summary>
        private bool MineBlockRegTest(MineBlockContext context)
        {
            context.ExtraNonce = this.IncrementExtraNonce(context.BlockTemplate.Block, context.ChainTip, context.ExtraNonce);

            Block block = context.BlockTemplate.Block;
            while ((context.MaxTries > 0) && (block.Header.Nonce < InnerLoopCount) && !block.CheckProofOfWork())
            {
                this.miningCancellationTokenSource.Token.ThrowIfCancellationRequested();

                ++block.Header.Nonce;
                --context.MaxTries;
            }

            if (context.MaxTries == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Ensures that the block was properly mined by checking the block's work against the next difficulty target.
        /// </summary>
        private bool ValidateMinedBlock(MineBlockContext context)
        {
            if (context.BlockTemplate.Block.Header.Nonce == InnerLoopCount)
                return false;

            var chainedHeader = new ChainedHeader(context.BlockTemplate.Block.Header, context.BlockTemplate.Block.GetHash(), context.ChainTip);
            if (chainedHeader.ChainWork <= context.ChainTip.ChainWork)
                return false;

            return true;
        }

        /// <summary>
        /// Validate the mined block by passing it to the consensus rule engine.
        /// <para>
        /// On successful block validation the block will be connected to the chain.
        /// </para>
        /// </summary>
        private bool ValidateAndConnectBlock(MineBlockContext context)
        {
            ChainedHeader chainedHeader = this.consensusManager.BlockMinedAsync(context.BlockTemplate.Block).GetAwaiter().GetResult();

            if (chainedHeader == null)
            {
                this.logger.LogTrace("(-)[BLOCK_VALIDATION_ERROR]:false");
                return false;
            }

            context.ChainedHeaderBlock = new ChainedHeaderBlock(context.BlockTemplate.Block, chainedHeader);

            return true;
        }

        private void OnBlockMined(MineBlockContext context)
        {
            this.logger.LogInformation("Mined new {0} block: '{1}'.", BlockStake.IsProofOfStake(context.ChainedHeaderBlock.Block) ? "POS" : "POW", context.ChainedHeaderBlock.ChainedHeader);

            context.CurrentHeight++;

            // memory leak...?
            context.Blocks.Add(context.BlockTemplate.Block.GetHash());
            context.BlockTemplate = null;
        }

        //<inheritdoc/>
        public int IncrementExtraNonce(Block block, ChainedHeader previousHeader, int extraNonce)
        {
            if (this.hashPrevBlock != block.Header.HashPrevBlock)
            {
                extraNonce = 0; // when the previous block changes, start extraNonce with 0
                this.hashPrevBlock = block.Header.HashPrevBlock;
            }

            // BIP34 requires the coinbase first input to start with the block height.
            int height = previousHeader.Height + 1;

            // Bitcoin Core appends the height and extra nonce in the following way:
            // txCoinbase.vin[0].scriptSig = (CScript() << nHeight << CScriptNum(nExtraNonce));
            var heightScriptBytes = new Script(Op.GetPushOp(height)).ToBytes();
            var extraNonceScriptBytes = new CScriptNum(extraNonce).getvch();
            var scriptSigBytes = new byte[heightScriptBytes.Length + extraNonceScriptBytes.Length];
            Buffer.BlockCopy(heightScriptBytes, 0, scriptSigBytes, 0, heightScriptBytes.Length);
            Buffer.BlockCopy(extraNonceScriptBytes, 0, scriptSigBytes, heightScriptBytes.Length, extraNonceScriptBytes.Length);

            block.Transactions[0].Inputs[0].ScriptSig = new Script(scriptSigBytes);

            this.blockProvider.BlockModified(previousHeader, block);

            Guard.Assert(block.Transactions[0].Inputs[0].ScriptSig.Length <= 100);

            return ++extraNonce; // increment and return new value
        }

        /// <summary>
        /// Context class that holds information on the current state of the mining process (per block).
        /// </summary>
        private class MineBlockContext
        {
            private readonly ulong amountOfBlocksToMine;
            public List<uint256> Blocks = new List<uint256>();
            public BlockTemplate BlockTemplate { get; set; }
            public ulong ChainHeight { get; set; }
            public ChainedHeaderBlock ChainedHeaderBlock { get; internal set; }
            public ulong CurrentHeight { get; set; }
            public ChainedHeader ChainTip { get; set; }
            public int ExtraNonce { get; set; }
            public ulong MaxTries { get; set; }
            public bool MiningCanContinue { get { return this.CurrentHeight < this.ChainHeight + this.amountOfBlocksToMine; } }
            public readonly ReserveScript ReserveScript;

            public MineBlockContext(ulong amountOfBlocksToMine, ulong chainHeight, ulong maxTries, ReserveScript reserveScript)
            {
                this.amountOfBlocksToMine = amountOfBlocksToMine;
                this.ChainHeight = chainHeight;
                this.CurrentHeight = chainHeight;
                this.MaxTries = maxTries;
                this.ReserveScript = reserveScript;
            }
        }

        /// <summary>
        /// CScriptNum implementation, taken from NBitcoin.
        /// </summary>
        public class CScriptNum
        {
            private const long nMaxNumSize = 4;
            /**
             * Numeric opcodes (OP_1ADD, etc) are restricted to operating on 4-byte integers.
             * The semantics are subtle, though: operands must be in the range [-2^31 +1...2^31 -1],
             * but results may overflow (and are valid as long as they are not used in a subsequent
             * numeric operation). CScriptNum enforces those semantics by storing results as
             * an int64 and allowing out-of-range values to be returned as a vector of bytes but
             * throwing an exception if arithmetic is done or the result is interpreted as an integer.
             */

            public CScriptNum(long n)
            {
                this.m_value = n;
            }
            private long m_value;

            public CScriptNum(byte[] vch, bool fRequireMinimal)
                : this(vch, fRequireMinimal, 4)
            {

            }
            public CScriptNum(byte[] vch, bool fRequireMinimal, long nMaxNumSize)
            {
                if (vch.Length > nMaxNumSize)
                {
                    throw new ArgumentException("script number overflow", nameof(vch));
                }
                if (fRequireMinimal && vch.Length > 0)
                {
                    // Check that the number is encoded with the minimum possible
                    // number of bytes.
                    //
                    // If the most-significant-byte - excluding the sign bit - is zero
                    // then we're not minimal. Note how this test also rejects the
                    // negative-zero encoding, 0x80.
                    if ((vch[vch.Length - 1] & 0x7f) == 0)
                    {
                        // One exception: if there's more than one byte and the most
                        // significant bit of the second-most-significant-byte is set
                        // it would conflict with the sign bit. An example of this case
                        // is +-255, which encode to 0xff00 and 0xff80 respectively.
                        // (big-endian).
                        if (vch.Length <= 1 || (vch[vch.Length - 2] & 0x80) == 0)
                        {
                            throw new ArgumentException("non-minimally encoded script number", nameof(vch));
                        }
                    }
                }

                this.m_value = set_vch(vch);
            }

            public override int GetHashCode()
            {
                return getint();
            }
            public override bool Equals(object obj)
            {
                if (!(obj is CScriptNum))
                    return false;
                var item = (CScriptNum)obj;
                return this.m_value == item.m_value;
            }
            public static bool operator ==(CScriptNum num, long rhs)
            {
                return num.m_value == rhs;
            }
            public static bool operator !=(CScriptNum num, long rhs)
            {
                return num.m_value != rhs;
            }
            public static bool operator <=(CScriptNum num, long rhs)
            {
                return num.m_value <= rhs;
            }
            public static bool operator <(CScriptNum num, long rhs)
            {
                return num.m_value < rhs;
            }
            public static bool operator >=(CScriptNum num, long rhs)
            {
                return num.m_value >= rhs;
            }
            public static bool operator >(CScriptNum num, long rhs)
            {
                return num.m_value > rhs;
            }

            public static bool operator ==(CScriptNum a, CScriptNum b)
            {
                return a.m_value == b.m_value;
            }
            public static bool operator !=(CScriptNum a, CScriptNum b)
            {
                return a.m_value != b.m_value;
            }
            public static bool operator <=(CScriptNum a, CScriptNum b)
            {
                return a.m_value <= b.m_value;
            }
            public static bool operator <(CScriptNum a, CScriptNum b)
            {
                return a.m_value < b.m_value;
            }
            public static bool operator >=(CScriptNum a, CScriptNum b)
            {
                return a.m_value >= b.m_value;
            }
            public static bool operator >(CScriptNum a, CScriptNum b)
            {
                return a.m_value > b.m_value;
            }

            public static CScriptNum operator +(CScriptNum num, long rhs)
            {
                return new CScriptNum(num.m_value + rhs);
            }
            public static CScriptNum operator -(CScriptNum num, long rhs)
            {
                return new CScriptNum(num.m_value - rhs);
            }
            public static CScriptNum operator +(CScriptNum a, CScriptNum b)
            {
                return new CScriptNum(a.m_value + b.m_value);
            }
            public static CScriptNum operator -(CScriptNum a, CScriptNum b)
            {
                return new CScriptNum(a.m_value - b.m_value);
            }

            public static CScriptNum operator &(CScriptNum a, long b)
            {
                return new CScriptNum(a.m_value & b);
            }
            public static CScriptNum operator &(CScriptNum a, CScriptNum b)
            {
                return new CScriptNum(a.m_value & b.m_value);
            }



            public static CScriptNum operator -(CScriptNum num)
            {
                assert(num.m_value != long.MinValue);
                return new CScriptNum(-num.m_value);
            }

            private static void assert(bool result)
            {
                if (!result)
                    throw new InvalidOperationException("Assertion fail for CScriptNum");
            }

            public static implicit operator CScriptNum(long rhs)
            {
                return new CScriptNum(rhs);
            }

            public static explicit operator long(CScriptNum rhs)
            {
                return rhs.m_value;
            }

            public static explicit operator uint(CScriptNum rhs)
            {
                return (uint)rhs.m_value;
            }



            public int getint()
            {
                if (this.m_value > int.MaxValue)
                    return int.MaxValue;
                else if (this.m_value < int.MinValue)
                    return int.MinValue;
                return (int)this.m_value;
            }

            public byte[] getvch()
            {
                return serialize(this.m_value);
            }

            private static byte[] serialize(long value)
            {
                if (value == 0)
                    return new byte[0];

                var result = new List<byte>();
                bool neg = value < 0;
                long absvalue = neg ? -value : value;

                while (absvalue != 0)
                {
                    result.Add((byte)(absvalue & 0xff));
                    absvalue >>= 8;
                }

                //    - If the most significant byte is >= 0x80 and the value is positive, push a
                //    new zero-byte to make the significant byte < 0x80 again.

                //    - If the most significant byte is >= 0x80 and the value is negative, push a
                //    new 0x80 byte that will be popped off when converting to an integral.

                //    - If the most significant byte is < 0x80 and the value is negative, add
                //    0x80 to it, since it will be subtracted and interpreted as a negative when
                //    converting to an integral.

                if ((result[result.Count - 1] & 0x80) != 0)
                    result.Add((byte)(neg ? 0x80 : 0));
                else if (neg)
                    result[result.Count - 1] |= 0x80;

                return result.ToArray();
            }

            private static long set_vch(byte[] vch)
            {
                if (vch.Length == 0)
                    return 0;

                long result = 0;
                for (int i = 0; i != vch.Length; ++i)
                    result |= ((long)(vch[i])) << 8 * i;

                // If the input vector's most significant byte is 0x80, remove it from
                // the result's msb and return a negative.
                if ((vch[vch.Length - 1] & 0x80) != 0)
                {
                    ulong temp = ~(0x80UL << (8 * (vch.Length - 1)));
                    return -((long)((ulong)result & temp));
                }

                return result;
            }
        }
    }
}
