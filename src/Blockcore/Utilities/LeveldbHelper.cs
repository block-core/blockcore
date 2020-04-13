using System;
using LevelDB;

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
    }
}