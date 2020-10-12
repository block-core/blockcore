using NBitcoin;

namespace Blockcore.Consensus.Transaction
{
    public class PrecomputedTransactionData
    {
        public PrecomputedTransactionData(Transaction tx)
        {
            this.HashOutputs = Script.Script.GetHashOutputs(tx);
            this.HashSequence = Script.Script.GetHashSequence(tx);
            this.HashPrevouts = Script.Script.GetHashPrevouts(tx);
        }

        public uint256 HashPrevouts
        {
            get;
            set;
        }

        public uint256 HashSequence
        {
            get;
            set;
        }

        public uint256 HashOutputs
        {
            get;
            set;
        }
    }
}