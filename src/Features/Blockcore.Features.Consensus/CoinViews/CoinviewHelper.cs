using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;

namespace Blockcore.Features.Consensus.CoinViews
{
    public class CoinviewHelper
    {
        /// <summary>
        /// Gets transactions identifiers that need to be fetched from store for specified block.
        /// </summary>
        /// <param name="block">The block with the transactions.</param>
        /// <param name="enforceBIP30">Whether to enforce look up of the transaction id itself and not only the reference to previous transaction id.</param>
        /// <returns>A list of transaction ids to fetch from store</returns>
        public OutPoint[] GetIdsToFetch(Block block, bool enforceBIP30)
        {
            var ids = new HashSet<OutPoint>();
            var trx = new HashSet<uint256>(); 
            foreach (Transaction tx in block.Transactions)
            {
                if (enforceBIP30)
                {
                    foreach (var utxo in tx.Outputs.AsIndexedOutputs())
                        ids.Add(utxo.ToOutPoint());
                }

                if (!tx.IsCoinBase)
                {
                    foreach (TxIn input in tx.Inputs)
                    {
                        // Check if an output is spend in the same block
                        // in case it was ignore it as no need to fetch it from disk.
                        // This extra hash list has a small overhead 
                        // but it's faster then fetching form disk an empty utxo.

                        if (!trx.Contains(input.PrevOut.Hash))
                            ids.Add(input.PrevOut);
                    }
                }

                trx.Add(tx.GetHash());
            }

            OutPoint[] res = ids.ToArray();
            return res;
        }
    }
}
