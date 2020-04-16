using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    /// <summary>
    /// An implementation of uint256 based on https://github.com/MithrilMan/MithrilShards
    /// Link to type https://github.com/MithrilMan/MithrilShards/blob/master/src/MithrilShards.Core/DataTypes/Uint256.cs
    /// Big credit to @MithrilMan for making this optimization
    /// </summary>
    public class uint256 : IEquatable<uint256>, IComparable<uint256>, IComparable
    {
        public class MutableUint256 : IBitcoinSerializable
        {
            private uint256 value;

            public uint256 Value
            {
                get
                {
                    return this.value;
                }
                set
                {
                    this.value = value;
                }
            }

            public uint256 MaxValue => uint256.Parse("ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");

            public MutableUint256()
            {
                this.value = Zero;
            }

            public MutableUint256(uint256 value)
            {
                this.value = value;
            }

            public void ReadWrite(BitcoinStream stream)
            {
                if (stream.Serializing)
                {
                    Span<byte> b = this.Value.ToSpan();
                    stream.ReadWrite(ref b);
                }
                else
                {
                    Span<byte> b = stackalloc byte[WidthByte];
                    stream.ReadWrite(ref b);
                    this.value = new uint256(b);
                }
            }
        }

        private const int ExpectedSize = 32;

        private const int Width = 256 / 64;

        private const int WidthByte = 256 / 8;

#pragma warning disable IDE0044 // Add readonly modifier
        private ulong part1;
        private ulong part2;
        private ulong part3;
        private ulong part4;
#pragma warning restore IDE0044 // Add readonly modifier

        internal ulong Part1 => this.part1;
        internal ulong Part2 => this.part2;
        internal ulong Part3 => this.part3;
        internal ulong Part4 => this.part4;

        public static uint256 Zero { get; } = new uint256();

        public static uint256 One { get; } = new uint256(1);

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

        public uint256(ReadOnlySpan<byte> input, bool littleEndian = true)
        {
            if (input.Length != ExpectedSize)
            {
                throw new FormatException("the byte array should be 32 bytes long");
            }

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, ExpectedSize / sizeof(ulong)));

            input.CopyTo(dst);

            if (!littleEndian)
            {
                dst.Reverse();
            }
        }

        public uint256(ulong b)
        {
            this.part1 = b;
            this.part2 = 0;
            this.part3 = 0;
            this.part4 = 0;
        }

        public uint256(byte[] payload, bool littleEndian = true)
        {
            if (payload.Length != WidthByte)
            {
                throw new FormatException("the byte array should be 256 byte long");
            }

            var input = new Span<byte>(payload);

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, ExpectedSize / sizeof(ulong)));

            input.CopyTo(dst);

            if (!littleEndian)
            {
                dst.Reverse();
            }
        }

        public uint256(byte[] payload) : this(payload, true)
        {
        }

        public uint256(string hexString)
        {
            if (hexString is null)
            {
                throw new ArgumentNullException(nameof(hexString));
            }

            //account for 0x prefix
            if (hexString.Length < ExpectedSize * 2)
            {
                throw new FormatException($"Invalid Hex String, the hex string should be {ExpectedSize * 2} chars long or {(ExpectedSize * 2) + 4} if prefixed with 0x.");
            }

            ReadOnlySpan<char> hexAsSpan = (hexString[0] == '0' && (hexString[1] == 'x' || hexString[1] == 'X')) ? hexString.Trim().AsSpan(2) : hexString.Trim().AsSpan();

            if (hexAsSpan.Length != ExpectedSize * 2)
            {
                throw new FormatException($"Invalid Hex String, the hex string should be {ExpectedSize * 2} chars long or {(ExpectedSize * 2) + 4} if prefixed with 0x.");
            }

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, ExpectedSize / sizeof(ulong)));

            int i = hexAsSpan.Length - 1;
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

        public static uint256 Parse(string hexString)
        {
            return new uint256(hexString);
        }

        private uint256(ulong[] array)
        {
            if (array.Length != Width)
            {
                throw new ArgumentOutOfRangeException();
            }

            this.part1 = array[0];
            this.part2 = array[1];
            this.part3 = array[2];
            this.part4 = array[3];
        }

        private ulong[] ToArray()
        {
            return new ulong[] { this.part1, this.part2, this.part3, this.part4, };
        }

        public static uint256 operator <<(uint256 a, int shift)
        {
            ulong[] source = a.ToArray();
            var target = new ulong[source.Length];
            int k = shift / 32;
            shift = shift % 32;
            for (int i = 0; i < Width; i++)
            {
                if (i + k + 1 < Width && shift != 0)
                    target[i + k + 1] |= (source[i] >> (32 - shift));
                if (i + k < Width)
                    target[i + k] |= (target[i] << shift);
            }
            return new uint256(target);
        }

        public static uint256 operator >>(uint256 a, int shift)
        {
            ulong[] source = a.ToArray();
            var target = new ulong[source.Length];
            int k = shift / 32;
            shift = shift % 32;
            for (int i = 0; i < Width; i++)
            {
                if (i - k - 1 >= 0 && shift != 0)
                    target[i - k - 1] |= (source[i] << (32 - shift));
                if (i - k >= 0)
                    target[i - k] |= (source[i] >> shift);
            }
            return new uint256(target);
        }

        public static bool TryParse(string hexString, out uint256 result)
        {
            try
            {
                result = new uint256(hexString);
                return true;
            }
            catch (Exception)
            {
                result = null;
            }

            return false;
        }

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

            return (byte)(value >> (byteIndex * 8));
        }

        public ReadOnlySpan<byte> ToReadOnlySpan()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref this.part1, ExpectedSize / sizeof(ulong)));
        }

        public Span<byte> ToSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.As<ulong, byte>(ref this.part1), ExpectedSize);
        }

        public byte[] ToBytes(bool littleEndian = true)
        {
            var output = this.ToSpan().ToArray();

            if (!littleEndian)
            {
                Span<byte> span = output.AsSpan();
                span.Reverse();
            }

            return output;
        }

        //public void ToBytes(Span<byte> output, bool lendian = true)
        //{
        //    if (output.Length < WIDTH_BYTE)
        //        throw new ArgumentException(message: $"The array should be at least of size {WIDTH_BYTE}", paramName: nameof(output));

        //    output = this.GetWritableBytes();

        //    if (!lendian)
        //        output.Reverse();
        //}

        public override int GetHashCode()
        {
            return (int)this.part1;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as uint256);
        }

        public static bool operator !=(uint256 a, uint256 b)
        {
            return !(a == b);
        }

        public static bool operator ==(uint256 a, uint256 b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        public bool Equals(uint256 other)
        {
            if (other is null)
                return false;

            return this.part1 == other.part1
                   && this.part2 == other.part2
                   && this.part3 == other.part3
                   && this.part4 == other.part4;
        }

        public int CompareTo(uint256 other)
        {
            return CompareTypes(this, other);
        }

        public int CompareTo(object obj)
        {
            switch (obj)
            {
                case uint256 target:
                    return this.CompareTo(target);

                case null:
                    return this.CompareTo(null as uint256);

                default:
                    throw new ArgumentException($"Object is not an instance of uint256", nameof(obj));
            }
        }

        public static bool operator <(uint256 a, uint256 b)
        {
            return CompareTypes(a, b) < 0;
        }

        public static bool operator >(uint256 a, uint256 b)
        {
            return CompareTypes(a, b) > 0;
        }

        public static bool operator <=(uint256 a, uint256 b)
        {
            return CompareTypes(a, b) <= 0;
        }

        public static bool operator >=(uint256 a, uint256 b)
        {
            return CompareTypes(a, b) >= 0;
        }

        public static int CompareTypes(uint256 a, uint256 b)
        {
            if (a is null && b is null)
                return 0;

            if (a is null && !(b is null))
                return -1;

            if (!(a is null) && b is null)
                return 1;

            if (a.part4 < b.part4) return -1;
            if (a.part4 > b.part4) return 1;
            if (a.part3 < b.part3) return -1;
            if (a.part3 > b.part3) return 1;
            if (a.part2 < b.part2) return -1;
            if (a.part2 > b.part2) return 1;
            if (a.part1 < b.part1) return -1;
            if (a.part1 > b.part1) return 1;

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

        public MutableUint256 AsBitcoinSerializable()
        {
            return new MutableUint256(this);
        }

        public override string ToString()
        {
            return string.Create(ExpectedSize * 2, this, (dst, src) =>
            {
                ReadOnlySpan<byte> rawData = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref src.part1, ExpectedSize / sizeof(ulong)));

                const string HexValues = "0123456789abcdef";

                int i = rawData.Length - 1;
                int j = 0;

                while (i >= 0)
                {
                    byte b = rawData[i--];
                    dst[j++] = HexValues[b >> 4];
                    dst[j++] = HexValues[b & 0xF];
                }
            });
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
                    var b = new byte[WidthByte];
                    stream.ReadWrite(ref b);
                    this._Value = new uint160(b);
                }
            }
        }

        public static uint160 Zero { get; } = new uint160();

        public static uint160 One { get; } = new uint160(1);

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
            if (hex.Length != WidthByte * 2)
                return false;
            if (!((HexEncoder)Encoders.Hex).IsValid(hex))
                return false;
            result = new uint160(hex);
            return true;
        }

        private static readonly HexEncoder Encoder = new HexEncoder();
        private const int WidthByte = 160 / 8;

        internal readonly uint pn0;
        internal readonly uint pn1;
        internal readonly uint pn2;
        internal readonly uint pn3;
        internal readonly uint pn4;

        public byte GetByte(int index)
        {
            int uintIndex = index / sizeof(uint);
            int byteIndex = index % sizeof(uint);
            uint value;
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
            if (vch.Length != WidthByte)
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
            if (bytes.Length != WidthByte)
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
            equals &= this.pn0 == other.pn0;
            equals &= this.pn1 == other.pn1;
            equals &= this.pn2 == other.pn2;
            equals &= this.pn3 == other.pn3;
            equals &= this.pn4 == other.pn4;
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
            var arr = new byte[WidthByte];
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
            return WidthByte;
        }

        public int Size
        {
            get
            {
                return WidthByte;
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