using LevelDB;

namespace System.Collections.Generic
{
    public static class DictionaryExtensions
    {
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
