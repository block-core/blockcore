using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NBitcoin;

namespace Blockcore.Benchmark.Uint256.New
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

        public uint256(ReadOnlySpan<byte> input)
        {
            if (input.Length != ExpectedSize)
            {
                throw new FormatException("the byte array should be 32 bytes long");
            }

            Span<byte> dst = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref this.part1, ExpectedSize / sizeof(ulong)));

            input.CopyTo(dst);
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
                    throw new ArgumentOutOfRangeException(nameof(index));
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
            var output = ToSpan().ToArray();

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
            return Equals(obj as uint256);
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
                    return CompareTo(target);

                case null:
                    return CompareTo(null);

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
}