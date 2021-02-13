﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.Consensus.Interfaces;
using Blockcore.Networks.X1.Consensus;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.Crypto;

namespace Blockcore.Networks.X1.Components
{
    public class X1StakeValidator : IStakeValidator
    {
        /// <summary>When checking the POS block signature this determines the maximum push data (public key) size following the OP_RETURN in the nonspendable output.</summary>
        private const int MaxPushDataSize = 40;

        // TODO: move this to IConsensus
        /// <summary>Time interval in minutes that is used in the retarget calculation.</summary>
        private const uint RetargetIntervalMinutes = 16;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Database of stake related data for the current blockchain.</summary>
        private readonly IStakeChain stakeChain;

        /// <summary>Thread safe access to the best chain of block headers (that the node is aware of) from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>Consensus' view of UTXO set.</summary>
        private readonly ICoinView coinView;

        /// <inheritdoc cref="Network"/>
        private readonly Network network;

        /// <inheritdoc />
        /// <param name="network">Specification of the network the node runs on - regtest/testnet/mainnet.</param>
        /// <param name="stakeChain">Database of stake related data for the current blockchain.</param>
        /// <param name="chainIndexer">Chain of headers.</param>
        /// <param name="coinView">Used for getting UTXOs.</param>
        /// <param name="loggerFactory">Factory for creating loggers.</param>
        public X1StakeValidator(Network network, IStakeChain stakeChain, ChainIndexer chainIndexer, ICoinView coinView, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.stakeChain = stakeChain;
            this.chainIndexer = chainIndexer;
            this.coinView = coinView;
            this.network = network;
        }

        /// <inheritdoc/>
        public ChainedHeader GetLastPowPosChainedBlock(IStakeChain stakeChain, ChainedHeader startChainedHeader, bool proofOfStake)
        {
            Guard.NotNull(stakeChain, nameof(stakeChain));
            Guard.Assert(startChainedHeader != null);

            BlockStake blockStake = stakeChain.Get(startChainedHeader.HashBlock);

            while ((startChainedHeader.Previous != null) && (blockStake.IsProofOfStake() != proofOfStake))
            {
                startChainedHeader = startChainedHeader.Previous;
                blockStake = stakeChain.Get(startChainedHeader.HashBlock);
            }

            return startChainedHeader;
        }

        /// <inheritdoc/>
        public Target CalculateRetarget(uint firstBlockTime, Target firstBlockTarget, uint secondBlockTime, BigInteger targetLimit)
        {
            uint targetSpacing = (uint)this.network.Consensus.TargetSpacing.TotalSeconds;
            uint actualSpacing = firstBlockTime > secondBlockTime ? firstBlockTime - secondBlockTime : targetSpacing;

            if (actualSpacing > targetSpacing * 10)
                actualSpacing = targetSpacing * 10;

            uint targetTimespan = RetargetIntervalMinutes * 60;
            uint interval = targetTimespan / targetSpacing;

            BigInteger target = firstBlockTarget.ToBigInteger();

            long multiplyBy = (interval - 1) * targetSpacing + actualSpacing + actualSpacing;
            target = target.Multiply(BigInteger.ValueOf(multiplyBy));

            long divideBy = (interval + 1) * targetSpacing;
            target = target.Divide(BigInteger.ValueOf(divideBy));

            this.logger.LogDebug("The next target difficulty will be {0} times higher (easier to satisfy) than the previous target.", (double)multiplyBy / (double)divideBy);

            if ((target.CompareTo(BigInteger.Zero) <= 0) || (target.CompareTo(targetLimit) >= 1))
                target = targetLimit;

            var finalTarget = new Target(target);

            return finalTarget;
        }

        /// <inheritdoc/>
        public Target GetNextTargetRequired(IStakeChain stakeChain, ChainedHeader chainTip, IConsensus consensus, bool proofOfStake)
        {
            Guard.NotNull(stakeChain, nameof(stakeChain));

            // If the chain uses a PosPowRatchet, we branch away here, 4 blocks after it has activated. A safe delta of 4
            // is used, so that when we iterate over blocks backwards, we'll never hit non-Ratchet blocks.
            if (consensus.Options is X1ConsensusOptions options &&
                options.IsPosPowRatchetActiveAtHeight(chainTip.Height - 4))
            {
                bool isChainTipProofOfStake = stakeChain.Get(chainTip.HashBlock).IsProofOfStake();
                if (isChainTipProofOfStake && chainTip.Height % 2 != 0 || !isChainTipProofOfStake && chainTip.Height % 2 == 0)
                    throw new InvalidOperationException("Misconfiguration: When the ratchet is active for a height, the convention that PoS block heights are even numbers, must be met.");

                return options.GetNextTargetRequired(chainTip, isChainTipProofOfStake, consensus, proofOfStake);
            }


            // Genesis block.
            if (chainTip == null)
            {
                this.logger.LogTrace("(-)[GENESIS]:'{0}'", consensus.PowLimit);
                return consensus.PowLimit;
            }

            // Find the last two blocks that correspond to the mining algo
            // (i.e if this is a POS block we need to find the last two POS blocks).
            BigInteger targetLimit = proofOfStake
                ? consensus.ProofOfStakeLimitV2
                : consensus.PowLimit.ToBigInteger();

            // First block.
            ChainedHeader lastPowPosBlock = this.GetLastPowPosChainedBlock(stakeChain, chainTip, proofOfStake);
            if (lastPowPosBlock.Previous == null)
            {
                var res = new Target(targetLimit);
                this.logger.LogTrace("(-)[FIRST_BLOCK]:'{0}'", res);
                return res;
            }

            // Second block.
            ChainedHeader prevLastPowPosBlock = this.GetLastPowPosChainedBlock(stakeChain, lastPowPosBlock.Previous, proofOfStake);
            if (prevLastPowPosBlock.Previous == null)
            {
                var res = new Target(targetLimit);
                this.logger.LogTrace("(-)[SECOND_BLOCK]:'{0}'", res);
                return res;
            }

            // This is used in tests to allow quickly mining blocks.
            if (!proofOfStake && consensus.PowNoRetargeting)
            {
                this.logger.LogTrace("(-)[NO_POW_RETARGET]:'{0}'", lastPowPosBlock.Header.Bits);
                return lastPowPosBlock.Header.Bits;
            }

            if (proofOfStake && consensus.PosNoRetargeting)
            {
                this.logger.LogTrace("(-)[NO_POS_RETARGET]:'{0}'", lastPowPosBlock.Header.Bits);
                return lastPowPosBlock.Header.Bits;
            }

            Target finalTarget = this.CalculateRetarget(lastPowPosBlock.Header.Time, lastPowPosBlock.Header.Bits, prevLastPowPosBlock.Header.Time, targetLimit);

            return finalTarget;
        }

        /// <inheritdoc/>
        public void CheckProofOfStake(PosRuleContext context, ChainedHeader prevChainedHeader, BlockStake prevBlockStake, Transaction transaction, uint headerBits)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(prevChainedHeader, nameof(prevChainedHeader));
            Guard.NotNull(prevBlockStake, nameof(prevBlockStake));
            Guard.NotNull(transaction, nameof(transaction));

            if (!transaction.IsCoinStake)
            {
                this.logger.LogTrace("(-)[NO_COINSTAKE]");
                ConsensusErrors.NonCoinstake.Throw();
            }

            TxIn txIn = transaction.Inputs[0];

            UnspentOutput prevUtxo = context.UnspentOutputSet.AccessCoins(txIn.PrevOut);
            if (prevUtxo == null)
            {
                this.logger.LogTrace("(-)[PREV_UTXO_IS_NULL]");
                ConsensusErrors.ReadTxPrevFailed.Throw();
            }

            // Verify signature.
            if (!this.VerifySignature(prevUtxo, transaction, 0, ScriptVerify.None))
            {
                this.logger.LogTrace("(-)[BAD_SIGNATURE]");
                ConsensusErrors.CoinstakeVerifySignatureFailed.Throw();
            }

            // Min age requirement.
            if (this.IsConfirmedInNPrevBlocks(prevUtxo, prevChainedHeader, this.GetTargetDepthRequired(prevChainedHeader)))
            {
                this.logger.LogTrace("(-)[BAD_STAKE_DEPTH]");
                ConsensusErrors.InvalidStakeDepth.Throw();
            }

            if (!this.CheckStakeKernelHash(context, headerBits, prevBlockStake.StakeModifierV2, prevUtxo, txIn.PrevOut, context.ValidationContext.ChainedHeaderToValidate.Header.Time))
            {
                this.logger.LogTrace("(-)[INVALID_STAKE_HASH_TARGET]");
                ConsensusErrors.StakeHashInvalidTarget.Throw();
            }
        }

        /// <inheritdoc/>
        public uint256 ComputeStakeModifierV2(ChainedHeader prevChainedHeader, uint256 prevStakeModifier, uint256 kernel)
        {
            Guard.NotNull(prevStakeModifier, nameof(prevStakeModifier));
            if (prevChainedHeader == null)
                return 0; // Genesis block's modifier is 0.

            uint256 stakeModifier;
            using (var ms = new MemoryStream())
            {
                var serializer = new BitcoinStream(ms, true);
                serializer.ReadWrite(kernel);
                serializer.ReadWrite(prevStakeModifier);
                stakeModifier = Hashes.Hash256(ms.ToArray());
            }

            return stakeModifier;
        }

        /// <inheritdoc/>
        public bool CheckKernel(PosRuleContext context, ChainedHeader prevChainedHeader, uint headerBits, long transactionTime, OutPoint prevout)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(prevout, nameof(prevout));
            Guard.NotNull(prevChainedHeader, nameof(prevChainedHeader));

            FetchCoinsResponse coins = this.coinView.FetchCoins(new[] { prevout });
            if ((coins == null) || (coins.UnspentOutputs.Count != 1))
            {
                this.logger.LogTrace("(-)[READ_PREV_TX_FAILED]");
                ConsensusErrors.ReadTxPrevFailed.Throw();
            }

            ChainedHeader prevBlock = this.chainIndexer.GetHeader(this.coinView.GetTipHash().Hash);
            if (prevBlock == null)
            {
                this.logger.LogTrace("(-)[REORG]");
                ConsensusErrors.ReadTxPrevFailed.Throw();
            }

            UnspentOutput prevUtxo = coins.UnspentOutputs.Single().Value;
            if (prevUtxo == null)
            {
                this.logger.LogTrace("(-)[PREV_UTXO_IS_NULL]");
                ConsensusErrors.ReadTxPrevFailed.Throw();
            }

            if (this.IsConfirmedInNPrevBlocks(prevUtxo, prevChainedHeader, this.GetTargetDepthRequired(prevChainedHeader)))
            {
                this.logger.LogTrace("(-)[LOW_COIN_AGE]");
                ConsensusErrors.InvalidStakeDepth.Throw();
            }

            BlockStake prevBlockStake = this.stakeChain.Get(prevChainedHeader.HashBlock);
            if (prevBlockStake == null)
            {
                this.logger.LogTrace("(-)[BAD_STAKE_BLOCK]");
                ConsensusErrors.BadStakeBlock.Throw();
            }

            return this.CheckStakeKernelHash(context, headerBits, prevBlockStake.StakeModifierV2, prevUtxo, prevout, (uint)transactionTime);
        }

        /// <inheritdoc/>
        public bool CheckStakeKernelHash(PosRuleContext context, uint headerBits, uint256 prevStakeModifier, UnspentOutput stakingCoins, OutPoint prevout, uint transactionTime)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(prevout, nameof(prevout));
            Guard.NotNull(stakingCoins, nameof(stakingCoins));

            if (transactionTime < stakingCoins.Coins.Time)
            {
                this.logger.LogDebug("Coinstake transaction timestamp {0} is lower than it's own UTXO timestamp {1}.", transactionTime, stakingCoins.Coins.Time);
                this.logger.LogTrace("(-)[BAD_STAKE_TIME]");
                ConsensusErrors.StakeTimeViolation.Throw();
            }

            // Base target.
            BigInteger target = new Target(headerBits).ToBigInteger();

            // Weighted target.
            long valueIn = stakingCoins.Coins.TxOut.Value.Satoshi;
            BigInteger weight = BigInteger.ValueOf(valueIn);
            BigInteger weightedTarget = target.Multiply(weight);

            context.TargetProofOfStake = this.ToUInt256(weightedTarget);
            this.logger.LogDebug("POS target is '{0}', weighted target for {1} coins is '{2}'.", this.ToUInt256(target), valueIn, context.TargetProofOfStake);

            // Calculate hash.
            using (var ms = new MemoryStream())
            {
                var serializer = new BitcoinStream(ms, true);
                serializer.ReadWrite(prevStakeModifier);
                if (this.network.Consensus.PosUseTimeFieldInKernalHash) // old posv3 time field
                    serializer.ReadWrite(stakingCoins.Coins.Time);
                serializer.ReadWrite(prevout.Hash);
                serializer.ReadWrite(prevout.N);
                serializer.ReadWrite(transactionTime);

                context.HashProofOfStake = Hashes.Hash256(ms.ToArray());
            }

            this.logger.LogDebug("Stake modifier V2 is '{0}', hash POS is '{1}'.", prevStakeModifier, context.HashProofOfStake);

            // Now check if proof-of-stake hash meets target protocol.
            var hashProofOfStakeTarget = new BigInteger(1, context.HashProofOfStake.ToBytes(false));
            if (hashProofOfStakeTarget.CompareTo(weightedTarget) > 0)
            {
                this.logger.LogTrace("(-)[TARGET_MISSED]");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public bool VerifySignature(UnspentOutput coin, Transaction txTo, int txToInN, ScriptVerify flagScriptVerify)
        {
            Guard.NotNull(coin, nameof(coin));
            Guard.NotNull(txTo, nameof(txTo));

            if (txToInN < 0 || txToInN >= txTo.Inputs.Count)
                return false;

            TxIn input = txTo.Inputs[txToInN];

            if (input.PrevOut.Hash != coin.OutPoint.Hash)
            {
                this.logger.LogTrace("(-)[INCORRECT_TX]");
                return false;
            }

            TxOut output = coin.Coins.TxOut;//.Outputs[input.PrevOut.N];

            if (output == null)
            {
                this.logger.LogTrace("(-)[OUTPUT_NOT_FOUND]");
                return false;
            }

            var txData = new PrecomputedTransactionData(txTo);
            var checker = new TransactionChecker(txTo, txToInN, output.Value, txData);
            var ctx = new ScriptEvaluationContext(this.chainIndexer.Network) { ScriptVerify = flagScriptVerify };

            bool res = ctx.VerifyScript(input.ScriptSig, output.ScriptPubKey, checker);
            return res;
        }

        /// <inheritdoc />
        public bool IsConfirmedInNPrevBlocks(UnspentOutput coins, ChainedHeader referenceChainedHeader, long targetDepth)
        {
            Guard.NotNull(coins, nameof(coins));
            Guard.NotNull(referenceChainedHeader, nameof(referenceChainedHeader));

            int actualDepth = referenceChainedHeader.Height - (int)coins.Coins.Height;
            bool res = actualDepth < targetDepth;

            return res;
        }

        /// <inheritdoc />
        public long GetTargetDepthRequired(ChainedHeader prevChainedHeader)
        {
            Guard.NotNull(prevChainedHeader, nameof(ChainedHeader));

            return ((X1ConsensusOptions) this.network.Consensus.Options).GetStakeMinConfirmations(prevChainedHeader.Height + 1, this.network) - 1;
        }

        /// <summary>
        /// Converts <see cref="BigInteger" /> to <see cref="uint256" />.
        /// </summary>
        /// <param name="input"><see cref="BigInteger"/> input value.</param>
        /// <returns><see cref="uint256"/> version of <paramref name="input"/>.</returns>
        private uint256 ToUInt256(BigInteger input)
        {
            byte[] array = input.ToByteArray();

            int missingZero = 32 - array.Length;

            if (missingZero < 0)
                return new uint256(array.Skip(Math.Abs(missingZero)).ToArray(), false);

            if (missingZero > 0)
                return new uint256(new byte[missingZero].Concat(array).ToArray(), false);

            return new uint256(array, false);
        }

        /// <inheritdoc />
        public bool CheckStakeSignature(BlockSignature signature, uint256 blockHash, Transaction coinStake)
        {
            if (signature.IsEmpty())
            {
                this.logger.LogTrace("(-)[EMPTY]:false");
                return false;
            }

            TxOut txout = coinStake.Outputs[1];

            if (PayToPubkeyTemplate.Instance.CheckScriptPubKey(txout.ScriptPubKey))
            {
                PubKey pubKey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(txout.ScriptPubKey);
                bool res = pubKey.Verify(blockHash, new ECDSASignature(signature.Signature));
                this.logger.LogTrace("(-)[P2PK]:{0}", res);
                return res;
            }

            // Block signing key also can be encoded in the nonspendable output.
            // This allows to not pollute UTXO set with useless outputs e.g. in case of multisig staking.

            List<Op> ops = txout.ScriptPubKey.ToOps().ToList();
            if (!ops.Any())
            {
                this.logger.LogTrace("(-)[NO_OPS]:false");
                return false;
            }

            if (ops.ElementAt(0).Code != OpcodeType.OP_RETURN) // OP_RETURN)
            {
                this.logger.LogTrace("(-)[NO_OP_RETURN]:false");
                return false;
            }

            if (ops.Count != 2)
            {
                this.logger.LogTrace("(-)[INVALID_OP_COUNT]:false");
                return false;
            }

            byte[] data = ops.ElementAt(1).PushData;

            if (data.Length > MaxPushDataSize)
            {
                this.logger.LogTrace("(-)[PUSH_DATA_TOO_LARGE]:false");
                return false;
            }

            if (!ScriptEvaluationContext.IsCompressedOrUncompressedPubKey(data))
            {
                this.logger.LogTrace("(-)[NO_PUSH_DATA]:false");
                return false;
            }

            bool verifyRes = new PubKey(data).Verify(blockHash, new ECDSASignature(signature.Signature));
            return verifyRes;
        }
    }
}
