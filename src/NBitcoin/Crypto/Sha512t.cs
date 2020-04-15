using System;
using System.Security.Cryptography;

namespace NBitcoin.Crypto
{
    public static class Sha512T
    {
        /// <summary>
        /// Truncated double-SHA512 hash. Used are the first 32 bytes of the second hash output.
        /// </summary>
        /// <seealso cref="https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf"/>
        /// <param name="src">bytes to hash</param>
        /// <returns>hash</returns>
        public static uint256 GetHash(byte[] src)
        {
            byte[] buffer32 = new byte[32];
            using (var sha512 = SHA512.Create())
            {
                var buffer64 = sha512.ComputeHash(src);
                buffer64 = sha512.ComputeHash(buffer64);
                Buffer.BlockCopy(buffer64, 0, buffer32, 0, 32);
            }

            return new uint256(buffer32);
        }
    }
}