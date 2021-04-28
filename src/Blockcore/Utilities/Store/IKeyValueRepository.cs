using System;

namespace Blockcore.Utilities.Store
{
    /// <summary>Allows saving and loading single values to and from key-value storage.</summary>
    public interface IKeyValueRepository : IDisposable
    {
        /// <summary>Persists byte array to the database.</summary>
        void SaveBytes(string key, byte[] bytes);

        /// <summary>Persists any object that <see cref="DataStoreSerializer"/> can serialize to the database.</summary>
        void SaveValue<T>(string key, T value);

        /// <summary>Persists any object to the database. Object is stored as JSON.</summary>
        void SaveValueJson<T>(string key, T value);

        /// <summary>Loads byte array from the database.</summary>
        byte[] LoadBytes(string key);

        /// <summary>Loads an object that <see cref="DataStoreSerializer"/> can deserialize from the database.</summary>
        T LoadValue<T>(string key);

        /// <summary>Loads JSON from the database and deserializes it.</summary>
        T LoadValueJson<T>(string key);
    }
}