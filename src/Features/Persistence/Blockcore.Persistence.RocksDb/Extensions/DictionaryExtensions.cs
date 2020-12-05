using RocksDbSharp;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
        public static Dictionary<byte[], byte[]> SelectDictionary(this RocksDb db, byte table)
        {
            var dict = new Dictionary<byte[], byte[]>();

            var enumerator = db.NewIterator();
            for (enumerator.SeekToFirst(); enumerator.Valid(); enumerator.Next())
            {
                if (enumerator.Key()[0] == table)
                    dict.Add(enumerator.Key().AsSpan().Slice(1).ToArray(), enumerator.Value());
            }

            return dict;
        }
    }
}
