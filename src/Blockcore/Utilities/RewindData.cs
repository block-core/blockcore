using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus.TransactionInfo;
using NBitcoin;

namespace Blockcore.Utilities
{
    /// <summary>
    /// Information about a previous state of the coinview that contains all information
    /// needed to rewind the coinview from the current state to the previous state.
    /// </summary>
    public class RewindData : IBitcoinSerializable
    {
        /// <summary>Hash of the block header of the tip of the previous state of the coinview.</summary>
        private HashHeightPair previousBlockHash;

        /// <summary>List of transaction IDs that needs to be removed when rewinding to the previous state as they haven't existed in the previous state.</summary>
        private List<OutPoint> outputsToRemove;

        /// <summary>List of unspent output transaction information that needs to be restored when rewinding to the previous state as they were fully spent in the current view.</summary>
        private List<RewindDataOutput> outputsToRestore;

        public RewindData()
        {
            this.outputsToRemove = new List<OutPoint>();
            this.outputsToRestore = new List<RewindDataOutput>();
        }

        public RewindData(HashHeightPair previousBlockHash) : this()
        {
            this.previousBlockHash = previousBlockHash;
        }

        // This value is not set after deserialization.
        public long TotalSize { get; set; }

        /// <summary>Hash of the block header of the tip of the previous state of the coinview.</summary>
        public HashHeightPair PreviousBlockHash
        {
            get { return this.previousBlockHash; }
            set { this.previousBlockHash = value; }
        }

        /// <summary>List of transaction IDs that needs to be removed when rewinding to the previous state as they haven't existed in the previous state.</summary>
        public List<OutPoint> OutputsToRemove
        {
            get { return this.outputsToRemove; }
            set { this.outputsToRemove = value; }
        }

        /// <summary>List of unspent output transaction information that needs to be restored when rewinding to the previous state as they were fully spent in the current view.</summary>
        public List<RewindDataOutput> OutputsToRestore
        {
            get { return this.outputsToRestore; }
            set { this.outputsToRestore = value; }
        }

        public override string ToString()
        {
            var data = new StringBuilder();

            data.AppendLine($"{nameof(this.previousBlockHash)}={this.previousBlockHash}");
            data.AppendLine($"{nameof(this.outputsToRemove)}.{nameof(this.outputsToRemove.Count)}={this.outputsToRemove.Count}:");

            foreach (OutPoint outputToRemove in this.outputsToRemove)
                data.AppendLine(outputToRemove.ToString());

            data.AppendLine($"{nameof(this.outputsToRestore)}.{nameof(this.outputsToRestore.Count)}={this.outputsToRestore.Count}:");

            foreach (RewindDataOutput output in this.outputsToRestore)
                data.AppendLine(output.ToString());

            return data.ToString();
        }

        /// <inheritdoc />
        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.previousBlockHash);
            stream.ReadWrite(ref this.outputsToRemove);
            stream.ReadWrite(ref this.outputsToRestore);
        }
    }

    public class RewindDataOutput : IBitcoinSerializable
    {
        private OutPoint outPoint;
        private Coins coins;

        public Coins Coins { get => this.coins; }

        public OutPoint OutPoint { get => this.outPoint; }

        public RewindDataOutput()
        {
        }

        public RewindDataOutput(OutPoint outPoint, Coins coins)
        {
            Guard.NotNull(outPoint, nameof(outPoint));
            Guard.NotNull(coins, nameof(coins));

            this.coins = coins;
            this.outPoint = outPoint;
        }

        public void ReadWrite(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.outPoint);
            stream.ReadWrite(ref this.coins);
        }
    }
}
