using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Consensus;
using Blockcore.Features.Consensus.CoinViews;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Networks;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.MemoryPool
{
    // TODO: Break this component in two.
    // The MempoolCoinView mixes functionality of fetching outputs from store and looking in the mempool.
    // It maybe be better to separate this in two differnet components, the whole notion (taken from bitcoin core)
    // of using a backing coinview (in this case for mempool) is not so relevant in the C# iplementation

    /// <summary>
    /// Memory pool coin view.
    /// Provides coin view representation of memory pool transactions via a backed coin view.
    /// </summary>
    public class MempoolCoinView : ICoinView
    {
        private readonly Network network;

        /// <summary>Transaction memory pool for managing transactions in the memory pool.</summary>
        /// <remarks>All access to this object has to be protected by <see cref="mempoolLock"/>.</remarks>
        private readonly ITxMempool memPool;

        /// <summary>A lock for protecting access to <see cref="memPool"/>.</summary>
        private readonly SchedulerLock mempoolLock;

        /// <summary>Memory pool validator for validating transactions.</summary>
        private readonly IMempoolValidator mempoolValidator;

        /// <summary>
        /// Constructs a memory pool coin view.
        /// </summary>
        /// <param name="inner">The backing coin view.</param>
        /// <param name="memPool">Transaction memory pool for managing transactions in the memory pool.</param>
        /// <param name="mempoolLock">A lock for managing asynchronous access to memory pool.</param>
        /// <param name="mempoolValidator">Memory pool validator for validating transactions.</param>
        public MempoolCoinView(Network network, ICoinView inner, ITxMempool memPool, SchedulerLock mempoolLock, IMempoolValidator mempoolValidator)
        {
            this.network = network;
            this.Inner = inner;
            this.memPool = memPool;
            this.mempoolLock = mempoolLock;
            this.mempoolValidator = mempoolValidator;
            this.Set = new UnspentOutputSet();
        }

        /// <summary>
        /// Gets the unspent transaction output set.
        /// </summary>
        public UnspentOutputSet Set { get; private set; }

        /// <summary>
        /// Backing coin view instance.
        /// </summary>
        public ICoinView Inner { get; }

        public void SaveChanges(IList<UnspentOutput> unspentOutputs, HashHeightPair HashHeightPair,
            HashHeightPair nextBlockHash, List<RewindData> rewindDataList = null)
        {
            throw new NotImplementedException();
        }

        public HashHeightPair GetTipHash()
        {
            throw new NotImplementedException();
        }

        public FetchCoinsResponse FetchCoins(OutPoint[] txIds)
        {
            throw new NotImplementedException();
        }

        public HashHeightPair Rewind()
        {
            throw new NotImplementedException();
        }

        public RewindData GetRewindData(int height)
        {
            throw new NotImplementedException();
        }

        public void CacheCoins(OutPoint[] utxos)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Load the coin view for a memory pool transaction. This should only be called
        /// inside the memory pool lock.
        /// </summary>
        /// <param name="trx">Memory pool transaction.</param>
        public void LoadViewLocked(Transaction trx)
        {
            // fetch outputs forn disk
            OutPoint[] outputs = trx.Inputs.Select(n => n.PrevOut).ToArray();
            FetchCoinsResponse coins = this.Inner.FetchCoins(outputs);

            // lookup all ids (duplicate ids are ignored in case a trx spends outputs from the same parent).
            List<uint256> ids = trx.Inputs.Select(n => n.PrevOut.Hash).Distinct().Concat(new[] { trx.GetHash() }).ToList();

            // find coins currently in the mempool
            foreach (uint256 trxid in ids)
            {
                if (this.memPool.MapTx.TryGetValue(trxid, out TxMempoolEntry entry))
                {
                    foreach (IndexedTxOut txOut in entry.Transaction.Outputs.AsIndexedOutputs())
                    {
                        // If an output was fetched form disk with empty coins but it
                        // was found mempool then override the output with whats in mempool

                        var outpoint = new OutPoint(trxid, txOut.N);
                        var found = coins.UnspentOutputs.TryGetValue(outpoint, out UnspentOutput unspentOutput);
                        if (!found || unspentOutput?.Coins == null)
                        {
                            if (unspentOutput?.Coins == null)
                                coins.UnspentOutputs.Remove(outpoint);

                            coins.UnspentOutputs.Add(outpoint, new UnspentOutput(outpoint, new Coins(TxMempool.MempoolHeight, txOut.TxOut, entry.Transaction.IsCoinBase, this.network.Consensus.IsProofOfStake ? entry.Transaction.IsCoinStake : false)));
                        }
                    }
                }
            }

            // the UTXO set might have been updated with a recently received block
            // but the block has not yet arrived to the mempool and remove the pending trx
            // from the pool (a race condition), block validation doesn't lock the mempool.
            // its safe to ignore duplicats on the UTXO set as duplicates mean a trx is in
            // a block and the block will soon remove the trx from the pool.
            this.Set.TrySetCoins(coins.UnspentOutputs.Values.ToArray());
        }

        /// <summary>
        /// Check whether a transaction id exists in the <see cref="TxMempool"/> or in the <see cref="MempoolCoinView"/>.
        /// </summary>
        /// <param name="txid">Transaction identifier.</param>
        /// <returns>Whether coins exist.</returns>
        public bool HaveTransaction(uint256 txid)
        {
            if (this.memPool.Exists(txid))
                return true;

            return this.Set.GetCoins(txid).Any();
        }

        /// <summary>
        /// Gets the priority of this memory pool transaction based upon chain height.
        /// </summary>
        /// <param name="tx">Memory pool transaction.</param>
        /// <param name="nHeight">Chain height.</param>
        /// <returns>Tuple of priority value and sum of all txin values that are already in blockchain.</returns>
        public (double priority, Money inChainInputValue) GetPriority(Transaction tx, int nHeight)
        {
            Money inChainInputValue = 0;
            if (tx.IsCoinBase)
                return (0.0, inChainInputValue);
            double dResult = 0.0;
            foreach (TxIn txInput in tx.Inputs)
            {
                UnspentOutput coins = this.Set.AccessCoins(txInput.PrevOut);

                if (coins == null)
                    continue;

                if (coins.Coins.Height <= nHeight)
                {
                    dResult += (double)coins.Coins.TxOut.Value.Satoshi * (nHeight - coins.Coins.Height);
                    inChainInputValue += coins.Coins.TxOut.Value;
                }
            }
            return (this.ComputePriority(tx, dResult), inChainInputValue);
        }

        /// <summary>
        /// Calculates the priority of a transaction based upon transaction size and priority inputs.
        /// </summary>
        /// <param name="trx">Memory pool transaction.</param>
        /// <param name="dPriorityInputs">Priority weighting of inputs.</param>
        /// <param name="nTxSize">Transaction size, 0 will compute.</param>
        /// <returns>Priority value.</returns>
        private double ComputePriority(Transaction trx, double dPriorityInputs, int nTxSize = 0)
        {
            nTxSize = MempoolValidator.CalculateModifiedSize(this.network.Consensus.ConsensusFactory, nTxSize, trx, this.mempoolValidator.ConsensusOptions);
            if (nTxSize == 0) return 0.0;

            return dPriorityInputs / nTxSize;
        }

        /// <summary>
        /// Whether memory pool transaction spends coin base.
        /// </summary>
        /// <param name="tx">Memory pool transaction.</param>
        /// <returns>Whether the transactions spends coin base.</returns>
        public bool SpendsCoinBase(Transaction tx)
        {
            foreach (TxIn txInput in tx.Inputs)
            {
                UnspentOutput coins = this.Set.AccessCoins(txInput.PrevOut);
                if (coins.Coins.IsCoinbase)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if an output exists.
        /// </summary>
        public bool HaveCoins(OutPoint outPoint)
        {
            return this.Set.AccessCoins(outPoint)?.Coins != null;
        }

        /// <summary>
        /// Gets the value of the inputs for a memory pool transaction.
        /// </summary>
        /// <param name="tx">Memory pool transaction.</param>
        /// <returns>Value of the transaction's inputs.</returns>
        public Money GetValueIn(Transaction tx)
        {
            return this.Set.GetValueIn(tx);
        }

        /// <summary>
        /// Gets the transaction output for a transaction input.
        /// </summary>
        /// <param name="input">Transaction input.</param>
        /// <returns>Transaction output.</returns>
        public TxOut GetOutputFor(TxIn input)
        {
            return this.Set.GetOutputFor(input);
        }
    }
}