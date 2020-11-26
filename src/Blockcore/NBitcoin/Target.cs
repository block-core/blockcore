using System;
using System.Buffers;
using System.Globalization;
using System.Linq;
using NBitcoin.BouncyCastle.Math;

namespace NBitcoin
{
    /// <summary>
    /// Represent the challenge that miners must solve for finding a new block
    /// </summary>
    public class Target
    {
        private static Target difficulty1 = new Target(new byte[] { 0x1d, 0x00, 0xff, 0xff });

        public static Target Difficulty1
        {
            get
            {
                return difficulty1;
            }
        }

        public Target(uint compact) : this(ToBytes(compact))
        {
        }

        private static byte[] ToBytes(uint bits)
        {
            return new byte[]
            {
                (byte)(bits >> 24),
                (byte)(bits >> 16),
                (byte)(bits >> 8),
                (byte)(bits)
            };
        }

        private BigInteger target;

        public Target(byte[] compact)
        {
            if (compact.Length == 4)
            {
                byte exp = compact[0];
                var val = new BigInteger(compact.SafeSubarray(1, 3));
                this.target = val.ShiftLeft(8 * (exp - 3));
            }
            else
            {
                throw new FormatException("Invalid number of bytes");
            }
        }

        public Target(BigInteger target)
        {
            this.target = target;
            this.target = new Target(ToCompact()).target;
        }

        public Target(uint256 target)
        {
            this.target = new BigInteger(target.ToBytes(false));
            this.target = new Target(ToCompact()).target;
        }

        public static implicit operator Target(uint a)
        {
            return new Target(a);
        }

        public static implicit operator uint(Target a)
        {
            byte[] bytes = a.target.ToByteArray();
            byte[] val = bytes.SafeSubarray(0, Math.Min(bytes.Length, 3));
            Array.Reverse(val);
            byte exp = (byte)(bytes.Length);
            if (exp == 1 && bytes[0] == 0)
                exp = 0;
            int missing = 4 - val.Length;
            if (missing > 0)
                val = val.Concat(new byte[missing]).ToArray();
            if (missing < 0)
                val = val.Take(-missing).ToArray();
            return (uint)val[0] + (uint)(val[1] << 8) + (uint)(val[2] << 16) + (uint)(exp << 24);
        }

        private double? difficulty;

        public double Difficulty
        {
            get
            {
                if (this.difficulty == null)
                {
                    BigInteger[] qr = Difficulty1.target.DivideAndRemainder(this.target);
                    BigInteger quotient = qr[0];
                    BigInteger remainder = qr[1];
                    BigInteger decimalPart = BigInteger.Zero;
                    for (int i = 0; i < 12; i++)
                    {
                        BigInteger div = (remainder.Multiply(BigInteger.Ten)).Divide(this.target);

                        decimalPart = decimalPart.Multiply(BigInteger.Ten);
                        decimalPart = decimalPart.Add(div);

                        remainder = remainder.Multiply(BigInteger.Ten).Subtract(div.Multiply(this.target));
                    }

                    this.difficulty = double.Parse(quotient.ToString() + "." + decimalPart.ToString(), new NumberFormatInfo()
                    {
                        NegativeSign = "-",
                        NumberDecimalSeparator = "."
                    });
                }
                return this.difficulty.Value;
            }
        }

        public override bool Equals(object obj)
        {
            var item = obj as Target;
            if (item == null)
                return false;
            return this.target.Equals(item.target);
        }

        public static bool operator ==(Target a, Target b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (((object)a == null) || ((object)b == null))
                return false;
            return a.target.Equals(b.target);
        }

        public static bool operator !=(Target a, Target b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return this.target.GetHashCode();
        }

        public BigInteger ToBigInteger()
        {
            return this.target;
        }

        public uint ToCompact()
        {
            return (uint)this;
        }

        public uint256 ToUInt256()
        {
            return ToUInt256(this.target);
        }

        internal static uint256 ToUInt256(BigInteger input)
        {
            return ToUInt256(input.ToByteArray());
        }

        internal static uint256 ToUInt256(byte[] array)
        {
            int missingZero = 32 - array.Length;

            if (missingZero < 0)
                throw new InvalidOperationException("Awful bug, this should never happen");

            if (missingZero != 0)
            {
                Span<byte> buffer = stackalloc byte[32];
                array.AsSpan().CopyTo(buffer.Slice(missingZero));
                return new uint256(buffer, false);
            }

            return new uint256(array, false);
        }

        public override string ToString()
        {
            return this.ToUInt256().ToString();
        }
    }
}