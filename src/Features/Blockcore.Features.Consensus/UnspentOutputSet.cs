using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Networks;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.Consensus
{
    public class UnspentOutputSet
    {
        private Dictionary<OutPoint, UnspentOutput> unspents;

        public TxOut GetOutputFor(TxIn txIn)
        {
            UnspentOutput unspent = this.unspents.TryGet(txIn.PrevOut);

            if (unspent?.Coins == null)
            {
                return null;
            }

            return unspent.Coins.TxOut;
        }

        public bool HaveInputs(Transaction tx)
        {
            return tx.Inputs.All(txin => this.GetOutputFor(txin) != null);
        }

        public UnspentOutput AccessCoins(OutPoint outpoint)
        {
            return this.unspents.TryGet(outpoint);
        }

        public Money GetValueIn(Transaction tx)
        {
            return tx.Inputs.Select(txin => this.GetOutputFor(txin).Value).Sum();
        }

        public void Update(Network network, Transaction transaction, int height)
        {
            if (!transaction.IsCoinBase)
            {
                foreach (TxIn input in transaction.Inputs)
                {
                    UnspentOutput unspentOutput = this.AccessCoins(input.PrevOut);

                    if (!unspentOutput.Spend())
                    {
                        throw new InvalidOperationException("Unspendable coins are invalid at this point");
                    }
                }
            }

            foreach (IndexedTxOut output in transaction.Outputs.AsIndexedOutputs())
            {
                var outpoint = output.ToOutPoint();
                var coinbase = transaction.IsCoinBase;
                var coinstake = network.Consensus.IsProofOfStake ? transaction.IsCoinStake : false;
                var time = (transaction is IPosTransactionWithTime posTx) ? posTx.Time : 0;
               
                var coins = new Coins((uint)height, output.TxOut, coinbase, coinstake, time);
                var unspentOutput = new UnspentOutput(outpoint, coins) 
                { 
                    CreatedFromBlock = true 
                };

                // If the output is an opreturn just ignore it
                if (coins.IsPrunable)
                    continue;

                // In cases where an output is spent in the same block
                // It will already exist as an input in the unspent list.
                this.unspents.AddOrReplace(outpoint, unspentOutput);
            }
        }

        public void SetCoins(UnspentOutput[] coins)
        {
            this.unspents = new Dictionary<OutPoint, UnspentOutput>(coins.Length);
            foreach (UnspentOutput coin in coins)
            {
                if (coin != null)
                {
                    this.unspents.Add(coin.OutPoint, coin);
                }
            }
        }

        public void TrySetCoins(UnspentOutput[] coins)
        {
            this.unspents = new Dictionary<OutPoint, UnspentOutput>(coins.Length);
            foreach (UnspentOutput coin in coins)
            {
                if (coin != null)
                {
                    this.unspents.TryAdd(coin.OutPoint, coin); 
                }
            }
        }

        public IList<UnspentOutput> GetCoins()
        {
            return this.unspents.Select(u => u.Value).ToList();
        }

        public IList<UnspentOutput> GetCoins(uint256 trxid)
        {
            return this.unspents.Where(w => w.Key.Hash == trxid).Select(u => u.Value).ToList();
        }
    }
}
