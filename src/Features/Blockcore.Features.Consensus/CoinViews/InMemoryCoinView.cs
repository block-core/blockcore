﻿using System;
using System.Collections.Generic;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus.CoinViews.Coindb;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.Consensus.CoinViews
{
    /// <summary>
    /// Coinview that holds all information in the memory, which is used in tests.
    /// </summary>
    /// <remarks>Rewinding is not supported in this implementation.</remarks>
    public class InMemoryCoinView : ICoinView, ICoindb
    {
        /// <summary>Lock object to protect access to <see cref="unspents"/> and <see cref="tipHash"/>.</summary>
        private readonly ReaderWriterLock lockobj = new ReaderWriterLock();

        /// <summary>Information about unspent outputs mapped by transaction IDs the outputs belong to.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private readonly Dictionary<OutPoint, UnspentOutput> unspents = new Dictionary<OutPoint, UnspentOutput>();

        /// <summary>Hash of the block header which is the tip of the coinview.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="lockobj"/>.</remarks>
        private HashHeightPair tipHash;

        /// <summary>
        /// Initializes an instance of the object.
        /// </summary>
        /// <param name="tipHash">Hash of the block headers of the tip of the coinview.</param>
        public InMemoryCoinView(HashHeightPair tipHash)
        {
            this.tipHash = tipHash;
        }

        /// <inheritdoc />
        public HashHeightPair GetTipHash()
        {
            return this.tipHash;
        }

        public void CacheCoins(OutPoint[] utxos)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public FetchCoinsResponse FetchCoins(OutPoint[] txIds)
        {
            Guard.NotNull(txIds, nameof(txIds));

            using (this.lockobj.LockRead())
            {
                var result = new FetchCoinsResponse();
                for (int i = 0; i < txIds.Length; i++)
                {
                    var output = this.unspents.TryGet(txIds[i]);

                    if (output != null)
                        result.UnspentOutputs.Add(output.OutPoint, output);
                }

                return result;
            }
        }

        /// <inheritdoc />
        public void SaveChanges(IList<UnspentOutput> unspentOutputs, HashHeightPair oldBlockHash, HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null)
        {
            Guard.NotNull(oldBlockHash, nameof(oldBlockHash));
            Guard.NotNull(nextBlockHash, nameof(nextBlockHash));
            Guard.NotNull(unspentOutputs, nameof(unspentOutputs));

            using (this.lockobj.LockWrite())
            {
                if ((this.tipHash != null) && (oldBlockHash != this.tipHash))
                    throw new InvalidOperationException("Invalid oldBlockHash");

                this.tipHash = nextBlockHash;
                foreach (UnspentOutput unspent in unspentOutputs)
                {
                    UnspentOutput existing;
                    if (this.unspents.TryGetValue(unspent.OutPoint, out existing))
                    {
                        existing.Spend();
                    }
                    else
                    {
                        existing = new UnspentOutput(unspent.OutPoint, unspent.Coins);
                        this.unspents.Add(unspent.OutPoint, existing);
                    }

                    if (existing.Coins?.IsPrunable ?? false)
                        this.unspents.Remove(unspent.OutPoint);
                }
            }
        }

        public HashHeightPair Rewind()
        {
            throw new NotImplementedException();
        }

        public RewindData GetRewindData(int height)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
        }
    }
}