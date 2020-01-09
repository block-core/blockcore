using System.Linq;
using System.Text;
using NBitcoin;
using NBitcoin.BitcoinCore;

namespace Stratis.Bitcoin.Utilities
{
    /// <summary>
    /// Represents an immutable instance of a transaction outout.
    /// To be used by coindb to serialize utxo to storage.
    /// </summary>
    public class Coins : IBitcoinSerializable
    {
        private uint height;
        private TxOut txOut;
        private bool isCoinbase;

        // Time and coinstake are pos properties, for POW they will stay default values.
        private bool isCoinstake;
        private uint time;

        public Coins()
        {
        }

        public Coins(uint height, TxOut txOut, bool isCoinbase, bool isCoinStake = false, uint time = 0)
        {
            Guard.NotNull(txOut, nameof(txOut));

            this.height = height;
            this.txOut = txOut;//.Clone();
            this.isCoinbase = isCoinbase;
            this.isCoinstake = isCoinStake;
            this.time = time;
        }

        public uint Height => this.height;

        public TxOut TxOut => this.txOut;

        public bool IsCoinbase => this.isCoinbase;

        public bool IsCoinstake => this.isCoinstake;

        public uint Time => this.time;


        public void ReadWrite(BitcoinStream stream)
        {
            if (stream.Serializing)
            {
                stream.ReadWriteAsVarInt(ref this.height);
                stream.ReadWrite(ref this.isCoinbase);
                stream.ReadWrite(ref this.isCoinstake);
                stream.ReadWrite(ref this.time);

                stream.ReadWrite(ref this.txOut);

                //var compressedTx = new TxOutCompressor(this.txOut);
                //stream.ReadWrite(ref compressedTx);
            }
            else
            {
                stream.ReadWriteAsVarInt(ref this.height);
                stream.ReadWrite(ref this.isCoinbase);
                stream.ReadWrite(ref this.isCoinstake);
                stream.ReadWrite(ref this.time);

                stream.ReadWrite(ref this.txOut);

                //var compressed = new TxOutCompressor();
                //stream.ReadWrite(ref compressed);
                //this.txOut = compressed.TxOut;
            }
        }

        public bool IsPrunable
        {
            get
            {
                return ((this.TxOut.ScriptPubKey.Length > 0) && (this.TxOut.ScriptPubKey.ToBytes(true)[0] == (byte)OpcodeType.OP_RETURN));
            }
        }
    }

    public class UnspentOutput
    {
        public UnspentOutput()
        {
        }

        public UnspentOutput(OutPoint outPoint, Coins coins)
        {
            Guard.NotNull(outPoint, nameof(outPoint));

            this.OutPoint = outPoint;
            this.Coins = coins;
        }

        public Coins Coins { get; private set; }

        public OutPoint OutPoint { get; private set; }

        public bool Spent { get; private set; }

        public bool MarkAsSpent()
        {
            this.Spent = true;

            // If Coins is null this means they are spent (or none existant)
            return this.Coins != null;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"{nameof(this.OutPoint)}={this.OutPoint}");

            if (this.Coins == null)
            {
                builder.AppendLine($"Coins=null");
                return builder.ToString();
            }

            builder.AppendLine($"{nameof(this.Coins)}.{nameof(this.Coins.Height)}={this.Coins.Height}");
            builder.AppendLine($"{nameof(this.Coins)}.{nameof(this.Coins.IsCoinbase)}={this.Coins.IsCoinbase}");
            builder.AppendLine($"{nameof(this.Coins)}.{nameof(this.Coins.IsCoinstake)}={this.Coins.IsCoinstake}");
            builder.AppendLine($"{nameof(this.Coins)}.{nameof(this.Coins.TxOut)}={this.Coins.TxOut}");

            return builder.ToString();
        }
    }
}
