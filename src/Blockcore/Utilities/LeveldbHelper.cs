using System;

namespace Blockcore.Utilities
{
    public static class DBH
    {
        public static byte[] Key(byte table, byte[] key)
        {
            Span<byte> dbkey = stackalloc byte[key.Length + 1];
            dbkey[0] = table;
            key.AsSpan().CopyTo(dbkey.Slice(1));
            return dbkey.ToArray();
        }

        public static byte[] Key(byte table, ReadOnlySpan<byte> key)
        {
            Span<byte> dbkey = stackalloc byte[key.Length + 1];
            dbkey[0] = table;
            key.CopyTo(dbkey.Slice(1));
            return dbkey.ToArray();
        }
    }
}