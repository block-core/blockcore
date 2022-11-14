using System;
using System.Linq;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Benchmark.Uint256.Old
{
    public class uint256 : IEquatable<uint256>, IComparable<uint256>, IComparable
    {
        public class MutableUint256 : IBitcoinSerializable
        {
            private uint256 _Value;

            public uint256 Value
            {
                get
                {
                    return this._Value;
                }
                set
                {
                    this._Value = value;
                }
            }

            public uint256 MaxValue => uint256.Parse("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");

            public MutableUint256()
            {
                this._Value = Zero;
            }

            public MutableUint256(uint256 value)
            {
                this._Value = value;
            }

            public void ReadWrite(BitcoinStream stream)
            {
                if (stream.Serializing)
                {
                    Span<byte> b = stackalloc byte[WIDTH_BYTE];
                    this.Value.ToBytes(b);
                    stream.ReadWrite(ref b);
                }
                else
                {
                    Span<byte> b = stackalloc byte[WIDTH_BYTE];
                    stream.ReadWrite(ref b);
                    this._Value = new uint256(b);
                }
            }
        }

        private static readonly uint256 _Zero = new uint256();

        public static uint256 Zero
        {
            get { return _Zero; }
        }

        private static readonly uint256 _One = new uint256(1);

        public static uint256 One
        {
            get { return _One; }
        }

        public uint256()
        {
        }

        public uint256(uint256 b)
        {
            this.pn0 = b.pn0;
            this.pn1 = b.pn1;
            this.pn2 = b.pn2;
            this.pn3 = b.pn3;
            this.pn4 = b.pn4;
            this.pn5 = b.pn5;
            this.pn6 = b.pn6;
            this.pn7 = b.pn7;
        }

        private const int WIDTH = 256 / 32;

        private uint256(uint[] array)
        {
            if (array.Length != WIDTH)
                throw new ArgumentOutOfRangeException();

            this.pn0 = array[0];
            this.pn1 = array[1];
            this.pn2 = array[2];
            this.pn3 = array[3];
            this.pn4 = array[4];
            this.pn5 = array[5];
            this.pn6 = array[6];
            this.pn7 = array[7];
        }

        private uint[] ToArray()
        {
            return new uint[] { this.pn0, this.pn1, this.pn2, this.pn3, this.pn4, this.pn5, this.pn6, this.pn7 };
        }

        public static uint256 operator <<(uint256 a, int shift)
        {
            uint[] source = a.ToArray();
            var target = new uint[source.Length];
            int k = shift / 32;
            shift = shift % 32;
            for (int i = 0; i < WIDTH; i++)
            {
                if (i + k + 1 < WIDTH && shift != 0)
                    target[i + k + 1] |= (source[i] >> (32 - shift));
                if (i + k < WIDTH)
                    target[i + k] |= (target[i] << shift);
            }
            return new uint256(target);
        }

        public static uint256 operator >>(uint256 a, int shift)
        {
            uint[] source = a.ToArray();
            var target = new uint[source.Length];
            int k = shift / 32;
            shift = shift % 32;
            for (int i = 0; i < WIDTH; i++)
            {
                if (i - k - 1 >= 0 && shift != 0)
                    target[i - k - 1] |= (source[i] << (32 - shift));
                if (i - k >= 0)
                    target[i - k] |= (source[i] >> shift);
            }
            return new uint256(target);
        }

        public static uint256 Parse(string hex)
        {
            return new uint256(hex);
        }

        public static bool TryParse(string hex, out uint256 result)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            result = null;
            if (hex.Length != WIDTH_BYTE * 2)
                return false;
            if (!((HexEncoder)Encoders.Hex).IsValid(hex))
                return false;
            result = new uint256(hex);
            return true;
        }

        private static readonly HexEncoder Encoder = new HexEncoder();
        private const int WIDTH_BYTE = 256 / 8;
        internal readonly UInt32 pn0;
        internal readonly UInt32 pn1;
        internal readonly UInt32 pn2;
        internal readonly UInt32 pn3;
        internal readonly UInt32 pn4;
        internal readonly UInt32 pn5;
        internal readonly UInt32 pn6;
        internal readonly UInt32 pn7;

        public byte GetByte(int index)
        {
            int uintIndex = index / sizeof(uint);
            int byteIndex = index % sizeof(uint);
            UInt32 value;
            switch (uintIndex)
            {
                case 0:
                    value = this.pn0;
                    break;

                case 1:
                    value = this.pn1;
                    break;

                case 2:
                    value = this.pn2;
                    break;

                case 3:
                    value = this.pn3;
                    break;

                case 4:
                    value = this.pn4;
                    break;

                case 5:
                    value = this.pn5;
                    break;

                case 6:
                    value = this.pn6;
                    break;

                case 7:
                    value = this.pn7;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
            return (byte)(value >> (byteIndex * 8));
        }

        public override string ToString()
        {
            var bytes = ToBytes();
            Array.Reverse(bytes);
            return Encoder.EncodeData(bytes);
        }

        public uint256(ulong b)
        {
            this.pn0 = (uint)b;
            this.pn1 = (uint)(b >> 32);
            this.pn2 = 0;
            this.pn3 = 0;
            this.pn4 = 0;
            this.pn5 = 0;
            this.pn6 = 0;
            this.pn7 = 0;
        }

        public uint256(byte[] vch, bool lendian = true)
        {
            if (vch.Length != WIDTH_BYTE)
            {
                throw new FormatException("the byte array should be 256 byte long");
            }

            if (!lendian)
                vch = vch.Reverse().ToArray();

            this.pn0 = Utils.ToUInt32(vch, 4 * 0, true);
            this.pn1 = Utils.ToUInt32(vch, 4 * 1, true);
            this.pn2 = Utils.ToUInt32(vch, 4 * 2, true);
            this.pn3 = Utils.ToUInt32(vch, 4 * 3, true);
            this.pn4 = Utils.ToUInt32(vch, 4 * 4, true);
            this.pn5 = Utils.ToUInt32(vch, 4 * 5, true);
            this.pn6 = Utils.ToUInt32(vch, 4 * 6, true);
            this.pn7 = Utils.ToUInt32(vch, 4 * 7, true);
        }

        public uint256(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != WIDTH_BYTE)
            {
                throw new FormatException("the byte array should be 32 bytes long");
            }

            this.pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
            this.pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
            this.pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
            this.pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
            this.pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
            this.pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
            this.pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
            this.pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
        }

        public uint256(string str)
        {
            this.pn0 = 0;
            this.pn1 = 0;
            this.pn2 = 0;
            this.pn3 = 0;
            this.pn4 = 0;
            this.pn5 = 0;
            this.pn6 = 0;
            this.pn7 = 0;
            str = str.Trim();

            if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                str = str.Substring(2);

            byte[] bytes = Encoder.DecodeData(str).Reverse().ToArray();
            if (bytes.Length != WIDTH_BYTE)
                throw new FormatException("Invalid hex length");
            this.pn0 = Utils.ToUInt32(bytes, 4 * 0, true);
            this.pn1 = Utils.ToUInt32(bytes, 4 * 1, true);
            this.pn2 = Utils.ToUInt32(bytes, 4 * 2, true);
            this.pn3 = Utils.ToUInt32(bytes, 4 * 3, true);
            this.pn4 = Utils.ToUInt32(bytes, 4 * 4, true);
            this.pn5 = Utils.ToUInt32(bytes, 4 * 5, true);
            this.pn6 = Utils.ToUInt32(bytes, 4 * 6, true);
            this.pn7 = Utils.ToUInt32(bytes, 4 * 7, true);
        }

        public uint256(byte[] vch)
            : this(vch, true)
        {
        }

        public override bool Equals(object obj)
        {
            var item = obj as uint256;
            return Equals(item);
        }

        public bool Equals(uint256 other)
        {
            if (other is null)
            {
                return false;
            }

            bool equals = true;
            equals &= this.pn0 == other.pn0;
            equals &= this.pn1 == other.pn1;
            equals &= this.pn2 == other.pn2;
            equals &= this.pn3 == other.pn3;
            equals &= this.pn4 == other.pn4;
            equals &= this.pn5 == other.pn5;
            equals &= this.pn6 == other.pn6;
            equals &= this.pn7 == other.pn7;
            return equals;
        }

        public int CompareTo(uint256 other)
        {
            return Comparison(this, other);
        }

        public int CompareTo(object obj)
        {
            return obj is uint256 v ? CompareTo(v) :
                   obj is null ? CompareTo(null) : throw new ArgumentException($"Object is not an instance of uint256", nameof(obj));
        }

        public static bool operator ==(uint256 a, uint256 b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (((object)a == null) || ((object)b == null))
                return false;

            bool equals = true;
            equals &= a.pn0 == b.pn0;
            equals &= a.pn1 == b.pn1;
            equals &= a.pn2 == b.pn2;
            equals &= a.pn3 == b.pn3;
            equals &= a.pn4 == b.pn4;
            equals &= a.pn5 == b.pn5;
            equals &= a.pn6 == b.pn6;
            equals &= a.pn7 == b.pn7;
            return equals;
        }

        public static bool operator <(uint256 a, uint256 b)
        {
            return Comparison(a, b) < 0;
        }

        public static bool operator >(uint256 a, uint256 b)
        {
            return Comparison(a, b) > 0;
        }

        public static bool operator <=(uint256 a, uint256 b)
        {
            return Comparison(a, b) <= 0;
        }

        public static bool operator >=(uint256 a, uint256 b)
        {
            return Comparison(a, b) >= 0;
        }

        public static int Comparison(uint256 a, uint256 b)
        {
            if (a is null && b is null)
                return 0;
            if (a is null && !(b is null))
                return -1;
            if (!(a is null) && b is null)
                return 1;
            if (a.pn7 < b.pn7)
                return -1;
            if (a.pn7 > b.pn7)
                return 1;
            if (a.pn6 < b.pn6)
                return -1;
            if (a.pn6 > b.pn6)
                return 1;
            if (a.pn5 < b.pn5)
                return -1;
            if (a.pn5 > b.pn5)
                return 1;
            if (a.pn4 < b.pn4)
                return -1;
            if (a.pn4 > b.pn4)
                return 1;
            if (a.pn3 < b.pn3)
                return -1;
            if (a.pn3 > b.pn3)
                return 1;
            if (a.pn2 < b.pn2)
                return -1;
            if (a.pn2 > b.pn2)
                return 1;
            if (a.pn1 < b.pn1)
                return -1;
            if (a.pn1 > b.pn1)
                return 1;
            if (a.pn0 < b.pn0)
                return -1;
            if (a.pn0 > b.pn0)
                return 1;
            return 0;
        }

        public static bool operator !=(uint256 a, uint256 b)
        {
            return !(a == b);
        }

        public static bool operator ==(uint256 a, ulong b)
        {
            return (a == new uint256(b));
        }

        public static bool operator !=(uint256 a, ulong b)
        {
            return !(a == new uint256(b));
        }

        public static implicit operator uint256(ulong value)
        {
            return new uint256(value);
        }

        public byte[] ToBytes(bool lendian = true)
        {
            var arr = new byte[WIDTH_BYTE];
            ToBytes(arr);
            if (!lendian)
                Array.Reverse(arr);
            return arr;
        }

        public void ToBytes(byte[] output)
        {
            Buffer.BlockCopy(Utils.ToBytes(this.pn0, true), 0, output, 4 * 0, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn1, true), 0, output, 4 * 1, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn2, true), 0, output, 4 * 2, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn3, true), 0, output, 4 * 3, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn4, true), 0, output, 4 * 4, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn5, true), 0, output, 4 * 5, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn6, true), 0, output, 4 * 6, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn7, true), 0, output, 4 * 7, 4);
        }

        public void ToBytes(Span<byte> output, bool lendian = true)
        {
            if (output.Length < WIDTH_BYTE)
                throw new ArgumentException(message: $"The array should be at least of size {WIDTH_BYTE}", paramName: nameof(output));

            Span<byte> initial = output;
            Utils.ToBytes(this.pn0, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn1, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn2, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn3, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn4, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn5, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn6, true, output);
            output = output.Slice(4);
            Utils.ToBytes(this.pn7, true, output);

            if (!lendian)
                initial.Reverse();
        }

        public MutableUint256 AsBitcoinSerializable()
        {
            return new MutableUint256(this);
        }

        public int GetSerializeSize()
        {
            return WIDTH_BYTE;
        }

        public int Size
        {
            get
            {
                return WIDTH_BYTE;
            }
        }

        public ulong GetLow64()
        {
            return this.pn0 | (ulong)this.pn1 << 32;
        }

        public uint GetLow32()
        {
            return this.pn0;
        }

        public override int GetHashCode()
        {
            return (int)this.pn0;
        }
    }
}