using System;
using System.Collections.Generic;
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

        public static Dictionary<byte[], byte[]> SelectDictionary(this DB db, byte table)
        {
            var dict = new Dictionary<byte[], byte[]>();

            var enumerator = db.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Key[0] == table)
                    dict.Add(enumerator.Current.Key.AsSpan().Slice(1).ToArray(), enumerator.Current.Value);
            }

            return dict;
        }
    }
}