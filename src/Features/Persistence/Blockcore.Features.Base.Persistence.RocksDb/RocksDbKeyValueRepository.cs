﻿using System.IO;
using System.Text;
using Blockcore.Configuration;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonConverters;
using Blockcore.Utilities.Store;
using RocksDbSharp;
using DB = RocksDbSharp.RocksDb;

namespace Blockcore.Features.Base.Persistence.RocksDb
{
    public class RocksDbKeyValueRepository : IKeyValueRepository
    {
        /// <summary>Access to database.</summary>
        private readonly DB rocksdb;

        private readonly DataStoreSerializer dataStoreSerializer;

        public RocksDbKeyValueRepository(DataFolder dataFolder, DataStoreSerializer dataStoreSerializer) : this(dataFolder.KeyValueRepositoryPath, dataStoreSerializer)
        {
        }

        public RocksDbKeyValueRepository(string folder, DataStoreSerializer dataStoreSerializer)
        {
            Directory.CreateDirectory(folder);
            this.dataStoreSerializer = dataStoreSerializer;

            // Open a connection to a new DB and create if not found
            var options = new DbOptions().SetCreateIfMissing(true);
            this.rocksdb = DB.Open(options, folder);
        }

        /// <inheritdoc />
        public void SaveBytes(string key, byte[] bytes)
        {
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);

            this.rocksdb.Put(keyBytes, bytes);
        }

        /// <inheritdoc />
        public void SaveValue<T>(string key, T value)
        {
            this.SaveBytes(key, this.dataStoreSerializer.Serialize(value));
        }

        /// <inheritdoc />
        public void SaveValueJson<T>(string key, T value)
        {
            string json = Serializer.ToString(value);
            byte[] jsonBytes = Encoding.ASCII.GetBytes(json);

            this.SaveBytes(key, jsonBytes);
        }

        /// <inheritdoc />
        public byte[] LoadBytes(string key)
        {
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);

            byte[] row = this.rocksdb.Get(keyBytes);

            if (row == null)
                return null;

            return row;
        }

        /// <inheritdoc />
        public T LoadValue<T>(string key)
        {
            byte[] bytes = this.LoadBytes(key);

            if (bytes == null)
                return default(T);

            T value = this.dataStoreSerializer.Deserialize<T>(bytes);
            return value;
        }

        /// <inheritdoc />
        public T LoadValueJson<T>(string key)
        {
            byte[] bytes = this.LoadBytes(key);

            if (bytes == null)
                return default(T);

            string json = Encoding.ASCII.GetString(bytes);

            T value = Serializer.ToObject<T>(json);

            return value;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.rocksdb.Dispose();
        }
    }
}