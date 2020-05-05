using System.Collections.Generic;
using System.Linq;
using Blockcore.Base.Deployments;
using Blockcore.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.MemoryPool.Interfaces;
using Blockcore.Features.Miner;
using Blockcore.Features.Miner.Comparers;
using Blockcore.Mining;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace x42.Networks.Consensus
{
    public class x42BlockDefinition : BlockDefinition
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        protected x42BlockDefinition(
           IConsensusManager consensusManager,
           IDateTimeProvider dateTimeProvider,
           ILoggerFactory loggerFactory,
           ITxMempool mempool,
           MempoolSchedulerLock mempoolLock,
           MinerSettings minerSettings,
           Network network,
           NodeDeployments nodeDeployments) : base(consensusManager, dateTimeProvider, loggerFactory, mempool, mempoolLock, minerSettings, network, nodeDeployments)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        public override void AddToBlock(TxMempoolEntry mempoolEntry)
        {
            base.AddTransactionToBlock(mempoolEntry.Transaction);
            base.UpdateBlockStatistics(mempoolEntry);
            base.UpdateTotalFees(mempoolEntry.Fee);
        }

        public override BlockTemplate Build(ChainedHeader chainTip, Script scriptPubKey)
        {
            base.OnBuild(chainTip, scriptPubKey);

            base.coinbase.Outputs[0].ScriptPubKey = new Script();
            base.coinbase.Outputs[0].Value = Money.Zero;

            return base.BlockTemplate;
        }

        public override void UpdateHeaders()
        {
            return;
        }

        public static bool IsOpReturn(byte[] bytes)
        {
            return bytes.Length > 0 && bytes[0] == (byte)OpcodeType.OP_RETURN;
        }

        protected override void AddTransactions(out int nPackagesSelected, out int nDescendantsUpdated)
        {
            nPackagesSelected = 0;
            nDescendantsUpdated = 0;

            // mapModifiedTx will store sorted packages after they are modified
            // because some of their txs are already in the block.
            var mapModifiedTx = new Dictionary<uint256, TxMemPoolModifiedEntry>();

            //var mapModifiedTxRes = this.mempoolScheduler.ReadAsync(() => mempool.MapTx.Values).GetAwaiter().GetResult();
            // mapModifiedTxRes.Select(s => new TxMemPoolModifiedEntry(s)).OrderBy(o => o, new CompareModifiedEntry());

            // Keep track of entries that failed inclusion, to avoid duplicate work.
            var failedTx = new TxMempool.SetEntries();

            // Start by adding all descendants of previously added txs to mapModifiedTx
            // and modifying them for their already included ancestors.
            UpdatePackagesForAdded(this.inBlock, mapModifiedTx);

            List<TxMempoolEntry> ancestorScoreList = this.MempoolLock.ReadAsync(() => this.Mempool.MapTx.AncestorScore).ConfigureAwait(false).GetAwaiter().GetResult().ToList();

            TxMempoolEntry iter;

            int nConsecutiveFailed = 0;
            while (ancestorScoreList.Any() || mapModifiedTx.Any())
            {
                TxMempoolEntry mi = ancestorScoreList.FirstOrDefault();
                if (mi != null)
                {
                    // Skip entries in mapTx that are already in a block or are present
                    // in mapModifiedTx (which implies that the mapTx ancestor state is
                    // stale due to ancestor inclusion in the block).
                    // Also skip transactions that we've already failed to add. This can happen if
                    // we consider a transaction in mapModifiedTx and it fails: we can then
                    // potentially consider it again while walking mapTx.  It's currently
                    // guaranteed to fail again, but as a belt-and-suspenders check we put it in
                    // failedTx and avoid re-evaluation, since the re-evaluation would be using
                    // cached size/sigops/fee values that are not actually correct.

                    // First try to find a new transaction in mapTx to evaluate.
                    if (mapModifiedTx.ContainsKey(mi.TransactionHash) || this.inBlock.Contains(mi) || failedTx.Contains(mi))
                    {
                        ancestorScoreList.Remove(mi);
                        continue;
                    }
                }

                // Now that mi is not stale, determine which transaction to evaluate:
                // the next entry from mapTx, or the best from mapModifiedTx?
                bool fUsingModified = false;
                TxMemPoolModifiedEntry modit;
                var compare = new CompareModifiedEntry();
                if (mi == null)
                {
                    modit = mapModifiedTx.Values.OrderBy(o => o, compare).First();
                    iter = modit.MempoolEntry;
                    fUsingModified = true;
                }
                else
                {
                    // Try to compare the mapTx entry to the mapModifiedTx entry
                    iter = mi;

                    modit = mapModifiedTx.Values.OrderBy(o => o, compare).FirstOrDefault();
                    if ((modit != null) && (compare.Compare(modit, new TxMemPoolModifiedEntry(iter)) < 0))
                    {
                        // The best entry in mapModifiedTx has higher score
                        // than the one from mapTx..
                        // Switch which transaction (package) to consider.

                        iter = modit.MempoolEntry;
                        fUsingModified = true;
                    }
                    else
                    {
                        // Either no entry in mapModifiedTx, or it's worse than mapTx.
                        // Increment mi for the next loop iteration.
                        ancestorScoreList.Remove(iter);
                    }
                }

                // We skip mapTx entries that are inBlock, and mapModifiedTx shouldn't
                // contain anything that is inBlock.
                Guard.Assert(!this.inBlock.Contains(iter));

                long packageSize = iter.SizeWithAncestors;
                Money packageFees = iter.ModFeesWithAncestors;
                long packageSigOpsCost = iter.SigOpCostWithAncestors;
                if (fUsingModified)
                {
                    packageSize = modit.SizeWithAncestors;
                    packageFees = modit.ModFeesWithAncestors;
                    packageSigOpsCost = modit.SigOpCostWithAncestors;
                }

                int opReturnCount = iter.Transaction.Outputs.Select(o => o.ScriptPubKey.ToBytes(true)).Count(b => IsOpReturn(b));

                // When there are zero fee's we want to still include the transaction.
                // If there is OP_RETURN data, we will want to make sure there is a fee.
                if (this.Network.MinTxFee > Money.Zero || opReturnCount > 0)
                {
                    if (packageFees < this.BlockMinFeeRate.GetFee((int)packageSize))
                    {
                        // Everything else we might consider has a lower fee rate
                        return;
                    }
                }

                if (!this.TestPackage(iter, packageSize, packageSigOpsCost))
                {
                    if (fUsingModified)
                    {
                        // Since we always look at the best entry in mapModifiedTx,
                        // we must erase failed entries so that we can consider the
                        // next best entry on the next loop iteration
                        mapModifiedTx.Remove(modit.MempoolEntry.TransactionHash);
                        failedTx.Add(iter);
                    }

                    nConsecutiveFailed++;

                    if ((nConsecutiveFailed > MaxConsecutiveAddTransactionFailures) && (this.BlockWeight > this.Options.BlockMaxWeight - 4000))
                    {
                        // Give up if we're close to full and haven't succeeded in a while
                        break;
                    }
                    continue;
                }

                var ancestors = new TxMempool.SetEntries();
                long nNoLimit = long.MaxValue;
                string dummy;

                this.MempoolLock.ReadAsync(() => this.Mempool.CalculateMemPoolAncestors(iter, ancestors, nNoLimit, nNoLimit, nNoLimit, nNoLimit, out dummy, false)).ConfigureAwait(false).GetAwaiter().GetResult();

                this.OnlyUnconfirmed(ancestors);
                ancestors.Add(iter);

                // Test if all tx's are Final.
                if (!this.TestPackageTransactions(ancestors))
                {
                    if (fUsingModified)
                    {
                        mapModifiedTx.Remove(modit.MempoolEntry.TransactionHash);
                        failedTx.Add(iter);
                    }
                    continue;
                }

                // This transaction will make it in; reset the failed counter.
                nConsecutiveFailed = 0;

                // Package can be added. Sort the entries in a valid order.
                // Sort package by ancestor count
                // If a transaction A depends on transaction B, then A's ancestor count
                // must be greater than B's.  So this is sufficient to validly order the
                // transactions for block inclusion.
                List<TxMempoolEntry> sortedEntries = ancestors.ToList().OrderBy(o => o, new CompareTxIterByAncestorCount()).ToList();
                foreach (TxMempoolEntry sortedEntry in sortedEntries)
                {
                    this.AddToBlock(sortedEntry);
                    // Erase from the modified set, if present
                    mapModifiedTx.Remove(sortedEntry.TransactionHash);
                }

                nPackagesSelected++;

                // Update transactions that depend on each of these
                nDescendantsUpdated += this.UpdatePackagesForAdded(ancestors, mapModifiedTx);
            }
        }

        /// <summary>
        /// Add descendants of given transactions to mapModifiedTx with ancestor
        /// state updated assuming given transactions are inBlock. Returns number
        /// of updated descendants.
        /// </summary>
        private int UpdatePackagesForAdded(TxMempool.SetEntries alreadyAdded, Dictionary<uint256, TxMemPoolModifiedEntry> mapModifiedTx)
        {
            int descendantsUpdated = 0;

            foreach (TxMempoolEntry addedEntry in alreadyAdded)
            {
                var setEntries = new TxMempool.SetEntries();

                this.MempoolLock.ReadAsync(() =>
                {
                    if (!this.Mempool.MapTx.ContainsKey(addedEntry.TransactionHash))
                    {
                        this.logger.LogWarning("{0} is not present in {1} any longer, skipping.", addedEntry.TransactionHash, nameof(this.Mempool.MapTx));
                        return;
                    }

                    this.Mempool.CalculateDescendants(addedEntry, setEntries);

                }).GetAwaiter().GetResult();

                foreach (TxMempoolEntry desc in setEntries)
                {
                    if (alreadyAdded.Contains(desc))
                        continue;

                    descendantsUpdated++;
                    TxMemPoolModifiedEntry modEntry;
                    if (!mapModifiedTx.TryGetValue(desc.TransactionHash, out modEntry))
                    {
                        modEntry = new TxMemPoolModifiedEntry(desc);
                        mapModifiedTx.Add(desc.TransactionHash, modEntry);
                        this.logger.LogDebug("Added transaction '{0}' to the block template because it's a required ancestor for '{1}'.", desc.TransactionHash, addedEntry.TransactionHash);
                    }

                    modEntry.SizeWithAncestors -= addedEntry.GetTxSize();
                    modEntry.ModFeesWithAncestors -= addedEntry.ModifiedFee;
                    modEntry.SigOpCostWithAncestors -= addedEntry.SigOpCost;
                }
            }

            return descendantsUpdated;
        }

        /// <summary>
        /// Remove confirmed <see cref="inBlock"/> entries from given set.
        /// </summary>
        private void OnlyUnconfirmed(TxMempool.SetEntries testSet)
        {
            foreach (TxMempoolEntry setEntry in testSet.ToList())
            {
                // Only test txs not already in the block
                if (this.inBlock.Contains(setEntry))
                {
                    testSet.Remove(setEntry);
                }
            }
        }

        /// <summary>
        /// Perform transaction-level checks before adding to block.
        /// <para>
        /// <list>
        /// <item>Transaction finality (locktime).</item>
        /// <item>Premature witness (in case segwit transactions are added to mempool before segwit activation).</item>
        /// <item>serialized size (in case -blockmaxsize is in use).</item>
        /// </list>
        /// </para>
        /// </summary>
        private bool TestPackageTransactions(TxMempool.SetEntries package)
        {
            foreach (TxMempoolEntry it in package)
            {
                if (!it.Transaction.IsFinal(Utils.UnixTimeToDateTime(this.LockTimeCutoff), this.height))
                    return false;

                if (!this.IncludeWitness && it.Transaction.HasWitness)
                    return false;

                if (this.NeedSizeAccounting)
                {
                    long nPotentialBlockSize = this.BlockSize; // only used with needSizeAccounting
                    int nTxSize = it.Transaction.GetSerializedSize();
                    if (nPotentialBlockSize + nTxSize >= this.Options.BlockMaxSize)
                        return false;

                    nPotentialBlockSize += nTxSize;
                }
            }

            return true;
        }
    }
}