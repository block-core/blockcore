using System;
using System.Linq;
using System.Runtime.InteropServices;
using NBitcoin.DataEncoders;

namespace NBitcoin
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
            this.part1 = b.part1;
            this.part2 = b.part2;
            this.part3 = b.part3;
            this.part4 = b.part4;
        }

        private const int EXPECTED_SIZE = 32;

        private const int WIDTH = 256 / 32;

        public uint256(ReadOnlySpan<byte> input)
        {
            if (input.Length != EXPECTED_SIZE)
            {
                throw new FormatException("the byte array should be 32 bytes long");
            }

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
            input.CopyTo(dst);
        }

        //private uint[] ToArray()
        //{
        //    return new ul[] { this.part1, this.part2, this.part3, this.part4, };
        //}

        //public static uint256 operator <<(uint256 a, int shift)
        //{
        //    uint[] source = a.ToArray();
        //    var target = new uint[source.Length];
        //    int k = shift / 32;
        //    shift = shift % 32;
        //    for (int i = 0; i < WIDTH; i++)
        //    {
        //        if (i + k + 1 < WIDTH && shift != 0)
        //            target[i + k + 1] |= (source[i] >> (32 - shift));
        //        if (i + k < WIDTH)
        //            target[i + k] |= (target[i] << shift);
        //    }
        //    return new uint256(target);
        //}

        //public static uint256 operator >>(uint256 a, int shift)
        //{
        //    uint[] source = a.ToArray();
        //    var target = new uint[source.Length];
        //    int k = shift / 32;
        //    shift = shift % 32;
        //    for (int i = 0; i < WIDTH; i++)
        //    {
        //        if (i - k - 1 >= 0 && shift != 0)
        //            target[i - k - 1] |= (source[i] << (32 - shift));
        //        if (i - k >= 0)
        //            target[i - k] |= (source[i] >> shift);
        //    }
        //    return new uint256(target);
        //}

        public static uint256 Parse(string hex)
        {
            return new uint256(hex);
        }

        public static bool TryParse(string hex, out uint256 result)
        {
            if (hex == null)
                throw new ArgumentNullException("hex");
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
#pragma warning disable IDE0044 // Add readonly modifier
        private ulong part1;
        private ulong part2;
        private ulong part3;
        private ulong part4;
#pragma warning restore IDE0044 // Add readonly modifier

        public byte GetByte(int index)
        {
            int uintIndex = index / sizeof(ulong);
            int byteIndex = index % sizeof(ulong);
            ulong value;
            switch (uintIndex)
            {
                case 0:
                    value = this.part1;
                    break;

                case 1:
                    value = this.part2;
                    break;

                case 2:
                    value = this.part3;
                    break;

                case 3:
                    value = this.part4;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("index");
            }
            return (byte)(value >> (byteIndex * 16));
        }

        public override string ToString()
        {
            var bytes = ToBytes();
            Array.Reverse(bytes);
            return Encoder.EncodeData(bytes);
        }

        public uint256(ulong b)
        {
            this.part1 = (uint)b;
            this.part2 = 0;
            this.part3 = 0;
            this.part4 = 0;
        }

        public uint256(byte[] vch, bool lendian = true)
        {
            if (vch.Length != WIDTH_BYTE)
            {
                throw new FormatException("the byte array should be 256 byte long");
            }

            if (!lendian)
                vch = vch.Reverse().ToArray();

            var input = new ReadOnlySpan<byte>(vch);

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
            input.CopyTo(dst);
        }

        public uint256(string hexString)
        {
            if (hexString is null)
            {
                throw new ArgumentNullException(nameof(hexString));
            }

            //account for 0x prefix
            if (hexString.Length < EXPECTED_SIZE * 2)
            {
                throw new FormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
            }

            ReadOnlySpan<char> hexAsSpan = (hexString[0] == '0' && hexString[1] == 'X') ? hexString.AsSpan(2) : hexString.AsSpan();

            if (hexString.Length != EXPECTED_SIZE * 2)
            {
                throw new FormatException($"the hex string should be {EXPECTED_SIZE * 2} chars long or {(EXPECTED_SIZE * 2) + 4} if prefixed with 0x.");
            }

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));

            int i = hexString.Length - 1;
            int j = 0;

            while (i > 0)
            {
                char c = hexAsSpan[i--];
                if (c >= '0' && c <= '9')
                {
                    dst[j] = (byte)(c - '0');
                }
                else if (c >= 'a' && c <= 'f')
                {
                    dst[j] = (byte)(c - ('a' - 10));
                }
                else if (c >= 'A' && c <= 'F')
                {
                    dst[j] = (byte)(c - ('A' - 10));
                }
                else
                {
                    throw new ArgumentException("Invalid nibble: " + c);
                }

                c = hexAsSpan[i--];
                if (c >= '0' && c <= '9')
                {
                    dst[j] |= (byte)((c - '0') << 4);
                }
                else if (c >= 'a' && c <= 'f')
                {
                    dst[j] |= (byte)((c - ('a' - 10)) << 4);
                }
                else if (c >= 'A' && c <= 'F')
                {
                    dst[j] |= (byte)((c - ('A' - 10)) << 4);
                }
                else
                {
                    throw new ArgumentException("Invalid nibble: " + c);
                }

                j++;
            }
        }

        public uint256(byte[] vch)
            : this(vch, true)
        {
        }

        public ReadOnlySpan<byte> GetBytes()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this.part1, EXPECTED_SIZE / sizeof(ulong)));
        }

        public override int GetHashCode()
        {
            return (int)this.part1;
        }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj) ? true : this.Equals(obj as uint256);

        public static bool operator !=(uint256? a, uint256? b) => !(a == b);

        public static bool operator ==(uint256? a, uint256? b) => a == null ? false : a.Equals(b);

        public bool Equals(uint256? other)
        {
            if (other is null) return false;

            return this.part1 == other.part1
                   && this.part2 == other.part2
                   && this.part3 == other.part3
                   && this.part4 == other.part4;
        }

        public int CompareTo(uint256 other)
        {
            return Comparison(this, other);
        }

        public int CompareTo(object obj)
        {
            return obj is uint256 v ? CompareTo(v) :
                   obj is null ? CompareTo(null as uint256) : throw new ArgumentException($"Object is not an instance of uint256", nameof(obj));
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
            if (a.part1 < b.part1)
                return -1;
            if (a.part2 > b.part2)
                return 1;
            if (a.part3 < b.part3)
                return -1;
            if (a.part4 > b.part4)
                return 1;
            return 0;
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
            var span = this.GetBytes();

            if (lendian)
                return span.ToArray();

            var reverseSpan = new Span<byte>();
            span.CopyTo(reverseSpan);
            reverseSpan.Reverse<byte>();
            return reverseSpan.ToArray();
        }

        public void ToBytes(Span<byte> output, bool lendian = true)
        {
            if (output.Length < WIDTH_BYTE)
                throw new ArgumentException(message: $"The array should be at least of size {WIDTH_BYTE}", paramName: nameof(output));

            var span = this.GetBytes();
            span.CopyTo(output);

            if (!lendian)
                output.Reverse();
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
            return this.part1;
        }

        public uint GetLow32()
        {
            return (uint)(this.part1 & uint.MaxValue);
        }
    }

    public class uint160 : IComparable<uint160>, IEquatable<uint160>, IComparable
    {
        public class MutableUint160 : IBitcoinSerializable
        {
            private uint160 _Value;

            public uint160 Value
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

            public MutableUint160()
            {
                this._Value = Zero;
            }

            public MutableUint160(uint160 value)
            {
                this._Value = value;
            }

            public void ReadWrite(BitcoinStream stream)
            {
                if (stream.Serializing)
                {
                    byte[] b = this.Value.ToBytes();
                    stream.ReadWrite(ref b);
                }
                else
                {
                    var b = new byte[WIDTH_BYTE];
                    stream.ReadWrite(ref b);
                    this._Value = new uint160(b);
                }
            }
        }

        private static readonly uint160 _Zero = new uint160();

        public static uint160 Zero
        {
            get { return _Zero; }
        }

        private static readonly uint160 _One = new uint160(1);

        public static uint160 One
        {
            get { return _One; }
        }

        public uint160()
        {
        }

        public uint160(uint160 b)
        {
            this.pn0 = b.pn0;
            this.pn1 = b.pn1;
            this.pn2 = b.pn2;
            this.pn3 = b.pn3;
            this.pn4 = b.pn4;
        }

        public static uint160 Parse(string hex)
        {
            return new uint160(hex);
        }

        public static bool TryParse(string hex, out uint160 result)
        {
            if (hex == null)
                throw new ArgumentNullException("hex");
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            result = null;
            if (hex.Length != WIDTH_BYTE * 2)
                return false;
            if (!((HexEncoder)Encoders.Hex).IsValid(hex))
                return false;
            result = new uint160(hex);
            return true;
        }

        private static readonly HexEncoder Encoder = new HexEncoder();
        private const int WIDTH_BYTE = 160 / 8;
        internal readonly UInt32 pn0;
        internal readonly UInt32 pn1;
        internal readonly UInt32 pn2;
        internal readonly UInt32 pn3;
        internal readonly UInt32 pn4;

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

                default:
                    throw new ArgumentOutOfRangeException("index");
            }
            return (byte)(value >> (byteIndex * 8));
        }

        public override string ToString()
        {
            return Encoder.EncodeData(ToBytes().Reverse().ToArray());
        }

        public uint160(ulong b)
        {
            this.pn0 = (uint)b;
            this.pn1 = (uint)(b >> 32);
            this.pn2 = 0;
            this.pn3 = 0;
            this.pn4 = 0;
        }

        public uint160(byte[] vch, bool lendian = true)
        {
            if (vch.Length != WIDTH_BYTE)
            {
                throw new FormatException("the byte array should be 160 byte long");
            }

            if (!lendian)
                vch = vch.Reverse().ToArray();

            this.pn0 = Utils.ToUInt32(vch, 4 * 0, true);
            this.pn1 = Utils.ToUInt32(vch, 4 * 1, true);
            this.pn2 = Utils.ToUInt32(vch, 4 * 2, true);
            this.pn3 = Utils.ToUInt32(vch, 4 * 3, true);
            this.pn4 = Utils.ToUInt32(vch, 4 * 4, true);
        }

        public uint160(string str)
        {
            this.pn0 = 0;
            this.pn1 = 0;
            this.pn2 = 0;
            this.pn3 = 0;
            this.pn4 = 0;
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
        }

        public uint160(byte[] vch)
            : this(vch, true)
        {
        }

        public override bool Equals(object obj)
        {
            var item = obj as uint160;
            return Equals(item);
        }

        public bool Equals(uint160 other)
        {
            if (other is null)
                return false;
            bool equals = true;
            equals &= pn0 == other.pn0;
            equals &= pn1 == other.pn1;
            equals &= pn2 == other.pn2;
            equals &= pn3 == other.pn3;
            equals &= pn4 == other.pn4;
            return equals;
        }

        public int CompareTo(uint160 other)
        {
            return Comparison(this, other);
        }

        public int CompareTo(object obj)
        {
            return obj is uint160 v ? CompareTo(v) :
                   obj is null ? CompareTo(null as uint160) : throw new ArgumentException($"Object is not an instance of uint160", nameof(obj));
        }

        public static bool operator ==(uint160 a, uint160 b)
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
            return equals;
        }

        public static bool operator <(uint160 a, uint160 b)
        {
            return Comparison(a, b) < 0;
        }

        public static bool operator >(uint160 a, uint160 b)
        {
            return Comparison(a, b) > 0;
        }

        public static bool operator <=(uint160 a, uint160 b)
        {
            return Comparison(a, b) <= 0;
        }

        public static bool operator >=(uint160 a, uint160 b)
        {
            return Comparison(a, b) >= 0;
        }

        private static int Comparison(uint160 a, uint160 b)
        {
            if (a is null && b is null)
                return 0;
            if (a is null && !(b is null))
                return -1;
            if (!(a is null) && b is null)
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

        public static bool operator !=(uint160 a, uint160 b)
        {
            return !(a == b);
        }

        public static bool operator ==(uint160 a, ulong b)
        {
            return (a == new uint160(b));
        }

        public static bool operator !=(uint160 a, ulong b)
        {
            return !(a == new uint160(b));
        }

        public static implicit operator uint160(ulong value)
        {
            return new uint160(value);
        }

        public byte[] ToBytes(bool lendian = true)
        {
            var arr = new byte[WIDTH_BYTE];
            Buffer.BlockCopy(Utils.ToBytes(this.pn0, true), 0, arr, 4 * 0, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn1, true), 0, arr, 4 * 1, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn2, true), 0, arr, 4 * 2, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn3, true), 0, arr, 4 * 3, 4);
            Buffer.BlockCopy(Utils.ToBytes(this.pn4, true), 0, arr, 4 * 4, 4);
            if (!lendian)
                Array.Reverse(arr);
            return arr;
        }

        public MutableUint160 AsBitcoinSerializable()
        {
            return new MutableUint160(this);
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