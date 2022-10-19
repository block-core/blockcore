using System;
using System.Linq;
using HashLib;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
    /// <summary>
    /// Type representation of data used in a bloom filter.
    /// </summary>
    public class Bloom : IBitcoinSerializable
    {
        /// <summary>
        /// Length of the bloom data in bytes. 2048 bits.
        /// </summary>
        public const int BloomLength = 256;

        /// <summary>
        /// The actual bloom value represented as a byte array.
        /// </summary>
        private byte[] data;

        public Bloom()
        {
            this.data = new byte[BloomLength];
        }

        public Bloom(byte[] data)
        {
            if (data?.Length != BloomLength)
                throw new ArgumentException($"Bloom byte array must be {BloomLength} bytes long.", nameof(data));

            this.data = CopyBloom(data);
        }

        /// <summary>
        /// Given this and another bloom, bitwise-OR all the data to get a bloom filter representing a range of data.
        /// </summary>
        public void Or(Bloom bloom)
        {
            for (int i = 0; i < this.data.Length; ++i)
            {
                this.data[i] |= bloom.data[i];
            }
        }

        /// <summary>
        /// Add some input to the bloom filter.
        /// </summary>
        /// <remarks>
        ///  From the Ethereum yellow paper (yellowpaper.io):
        ///  M3:2048 is a specialised Bloom filter that sets three bits
        ///  out of 2048, given an arbitrary byte series. It does this through
        ///  taking the low-order 11 bits of each of the first three pairs of
        ///  bytes in a Keccak-256 hash of the byte series.
        /// </remarks>
        public void Add(byte[] input)
        {
            byte[] hashBytes = Keccak256(input);
            // for first 3 pairs, calculate value of first 11 bits
            for (int i = 0; i < 6; i += 2)
            {
                uint low8Bits = (uint)hashBytes[i + 1];
                uint high3Bits = ((uint)hashBytes[i] << 8) & 2047; // AND with 2047 wipes any bits higher than our desired 11.
                uint index = low8Bits + high3Bits;
                this.SetBit((int)index);
            }
        }

        /// <summary>
        /// Returns a 32-byte Keccak256 hash of the given bytes.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] Keccak256(byte[] input)
        {
            return HashFactory.Crypto.SHA3.CreateKeccak256().ComputeBytes(input).GetBytes();
        }

        /// <summary>
        /// Determine whether some input is possibly contained within the filter.
        /// </summary>
        /// <param name="test">The byte array to test.</param>
        /// <returns>Whether this data could be contained within the filter.</returns>
        public bool Test(byte[] test)
        {
            var compare = new Bloom();
            compare.Add(test);
            return this.Test(compare);
        }

        /// <summary>
        /// Determine whether a second bloom is possibly contained within the filter.
        /// </summary>
        /// <param name="bloom">The second bloom to test.</param>
        /// <returns>Whether this data could be contained within the filter.</returns>
        public bool Test(Bloom bloom)
        {
            var copy = new Bloom(bloom.ToBytes());
            copy.Or(this);
            return this.Equals(copy);
        }

        /// <summary>
        /// Sets the specific bit to 1 within our 256-byte array.
        /// </summary>
        /// <param name="index">Index (0-2047) of the bit to assign to 1.</param>
        private void SetBit(int index)
        {
            int byteIndex = index / 8;
            int bitInByteIndex = index % 8;
            byte mask = (byte)(1 << bitInByteIndex);
            this.data[byteIndex] |= mask;
        }

        public void ReadWrite(BitcoinStream stream)
        {
            if (stream.Serializing)
            {
                byte[] b = CopyBloom(this.data);
                stream.ReadWrite(ref b);
            }
            else
            {
                var b = new byte[BloomLength];
                stream.ReadWrite(ref b);
                this.data = b;
            }
        }

        /// <summary>
        /// Returns the raw bytes of this filter.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return CopyBloom(this.data);
        }

        public override string ToString()
        {
            return Encoders.Hex.EncodeData(this.data);
        }

        public static bool operator ==(Bloom obj1, Bloom obj2)
        {
            if (object.ReferenceEquals(obj1, null))
                return object.ReferenceEquals(obj2, null);

            return Enumerable.SequenceEqual(obj1.data, obj2.data);
        }

        public static bool operator !=(Bloom obj1, Bloom obj2)
        {
            return !(obj1 == obj2);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Bloom);
        }

        public bool Equals(Bloom obj)
        {
            if (object.ReferenceEquals(obj, null))
                return false;

            if (object.ReferenceEquals(this, obj))
                return true;

            return (obj == this);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.data);
        }

        private static byte[] CopyBloom(byte[] bloom)
        {
            var result = new byte[BloomLength];
            Buffer.BlockCopy(bloom, 0, result, 0, BloomLength);
            return result;
        }

        /// <summary>
        /// Compresses a bloom filter to the following encoding:
        ///   (length of encoding) [[(number of zeros)(explicit byte) ...]
        /// </summary>
        /// <param name="maxSize">The maximum size of the compressed bytes.</param>
        /// <returns>The compressed bytes  or <c>null</c> if <paramref name="maxSize"/> is exceeded.</returns>
        public byte[] GetCompressedBloom()
        {
            // The compressed version should be shorter than the uncompressed version.
            int maxSize = BloomLength - 1;

            var b = this.ToBytes();
            var c = new byte[maxSize];
            byte zeros = 0;
            int j = 0;
            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] != 0 || zeros == byte.MaxValue)
                {
                    if (j >= (maxSize - 1))
                        return b;

                    c[j++] = (byte)zeros;
                    c[j++] = b[i];
                    zeros = 0;
                }
                else if (zeros < byte.MaxValue)
                {
                    zeros++;
                }
            }

            if (zeros != 0)
            {
                if (j >= maxSize)
                    return b;

                c[j++] = zeros;
            }

            var res = new byte[j];

            Array.Copy(c, res, j);

            return res;
        }

        /// <summary>
        /// Derives a bloom filter by decompressing the following encoding:
        ///   (length of encoding) [[(number of zeros)(explicit byte) ...]
        /// </summary>
        /// <param name="data">The data to decompress.</param>
        /// <returns>The bloom object.</returns>
        public static Bloom GetDecompressedBloom(byte[] data)
        {
            if (data.Length == Bloom.BloomLength)
                return new Bloom(data);

            var b = new byte[Bloom.BloomLength];
            int j = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                int zeros = data[i];
                while (zeros-- > 0)
                    b[j++] = 0;

                if ((i + 1) < data.Length)
                    b[j++] = data[i + 1];
            }

            if (j != Bloom.BloomLength)
                throw new InvalidOperationException("The decompressed bloom filter is not the expected length.");

            return new Bloom(b);
        }

        public int GetCompressedSize()
        {
            return this.GetCompressedBloom().Length + 1;
        }
    }

    public static class BloomStreamExt
    {
        public static void ReadWriteCompressed(this BitcoinStream stream, ref Bloom bloom)
        {
            // Ensure that the length can be serialized using a single byte.
            const int maxSerializedSize = byte.MaxValue + 1;
            if (Bloom.BloomLength > maxSerializedSize)
                throw new InvalidOperationException($"'{nameof(ReadWriteCompressed)}' does not support bloom filters greater than {maxSerializedSize} bytes in length.");

            if (stream.Serializing)
            {   // Writing to stream.
                byte[] ser = bloom.GetCompressedBloom();
                byte len = (byte)(ser.Length - 1);
                stream.ReadWrite(ref len);
                stream.ReadWrite(ref ser);
            }
            else
            {   // Reading from stream.
                byte len = 0;
                stream.ReadWrite(ref len);

                // A value of 0 can be used to support larger blooms in the future. For now its not supported.
                if (len == 0)
                    throw new NotImplementedException("The bloom compression format is not supported.");

                var c = new byte[(int)len + 1];
                stream.ReadWrite(ref c);
                bloom = Bloom.GetDecompressedBloom(c);
            }
        }
    }
}
