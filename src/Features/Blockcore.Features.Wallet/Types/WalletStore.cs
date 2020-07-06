using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Blockcore.Configuration;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonConverters;
using LiteDB;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Types
{
    public interface IWalletStore
    {
        void InsertOrUpdate(TransactionData item);

        IEnumerable<TransactionData> GetForAddress(string address);

        IEnumerable<TransactionData> GetUnspentForAddress(string address);

        int CountForAddress(string address);

        TransactionData GetForOutput(OutPoint outPoint);

        bool Remove(OutPoint outPoint);

        WalletData GetData();

        void SetData(WalletData data);
    }

    public class WalletData
    {
        [BsonId]
        public string Key { get; set; }

        public string EncryptedSeed { get; set; }

        public string WalletName { get; set; }

        public HashHeightPair WalletTip { get; set; }

        public ICollection<uint256> BlockLocator { get; set; }
    }

    public class WalletStore : IWalletStore, IDisposable
    {
        private LiteDatabase db;
        private readonly Network network;
        private LiteCollection<WalletData> dataCol;
        private LiteCollection<TransactionData> trxCol;

        public WalletData WalletData { get; private set; }

        public BsonMapper Mapper => this.db.Mapper;

        public WalletStore(Network network, DataFolder dataFolder, Wallet wallet)
        {
            string dbPath = Path.Combine(dataFolder.WalletFolderPath, $"{wallet.Name}.txdb.litedb");

            if (!Directory.Exists(dataFolder.WalletFolderPath))
            {
                Directory.CreateDirectory(dataFolder.WalletFolderPath);
            }

            BsonMapper mapper = this.Create();
            LiteDB.FileMode fileMode = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? LiteDB.FileMode.Exclusive : LiteDB.FileMode.Shared;
            this.db = new LiteDatabase(new ConnectionString() { Filename = dbPath, Mode = fileMode }, mapper: mapper);

            this.trxCol = this.db.GetCollection<TransactionData>("transactions");
            this.dataCol = this.db.GetCollection<WalletData>("data");

            this.trxCol.EnsureIndex(x => x.OutPoint, true);
            this.trxCol.EnsureIndex(x => x.Address, false);
            this.trxCol.EnsureIndex(x => x.BlockHeight, false);

            this.dataCol.EnsureIndex(x => x.Key, true);

            this.WalletData = this.GetData();

            if (this.WalletData != null)
            {
                if (this.WalletData.EncryptedSeed != wallet.EncryptedSeed)
                {
                    throw new WalletException("Invalid Wallet seed");
                }
            }
            else
            {
                this.SetData(new WalletData
                {
                    Key = "Key",
                    EncryptedSeed = wallet.EncryptedSeed,
                    WalletName = wallet.Name,
                    WalletTip = new HashHeightPair(network.GenesisHash, 0)
                });
            }

            this.network = network;
        }

        public WalletData GetData()
        {
            if (this.WalletData == null)
            {
                this.WalletData = this.dataCol.FindById("Key");
            }

            return this.WalletData;
        }

        public void SetData(WalletData data)
        {
            this.dataCol.Upsert(data);
            this.WalletData = data;
        }

        public int CountForAddress(string address)
        {
            var count = this.trxCol.Count(Query.EQ("Address", new BsonValue(address)));
            return count;
        }

        public void InsertOrUpdate(TransactionData item)
        {
            this.trxCol.Upsert(item);
        }

        public IEnumerable<TransactionData> GetForAddress(string address)
        {
            var trxs = this.trxCol.Find(Query.EQ("Address", new BsonValue(address)));
            return trxs;
        }

        public IEnumerable<TransactionData> GetUnspentForAddress(string address)
        {
            var trxs = this.trxCol.Find(Query.And(Query.EQ("Address", new BsonValue(address)), Query.EQ("SpendingDetails", BsonValue.Null)));
            return trxs;
        }

        public TransactionData GetForOutput(OutPoint outPoint)
        {
            var trx = this.trxCol.FindById(outPoint.ToString());
            return trx;
        }

        public bool Remove(OutPoint outPoint)
        {
            return this.trxCol.Delete(outPoint.ToString());
        }

        private BsonMapper Create()
        {
            var mapper = new BsonMapper();

            mapper.RegisterType<HashHeightPair>
            (
                serialize: (hash) => hash.ToString(),
                deserialize: (bson) => HashHeightPair.Parse(bson.AsString)
            );

            mapper.RegisterType<OutPoint>
            (
                serialize: (outPoint) => outPoint.ToString(),
                deserialize: (bson) => OutPoint.Parse(bson.AsString)
            );

            mapper.RegisterType<uint256>
            (
                serialize: (hash) => hash.ToString(),
                deserialize: (bson) => uint256.Parse(bson.AsString)
            );

            mapper.RegisterType<Money>
            (
                serialize: (money) => money.Satoshi,
                deserialize: (bson) => Money.Satoshis(bson.AsInt64)
            );

            mapper.RegisterType<Script>
            (
                serialize: (script) => Encoders.Hex.EncodeData(script.ToBytes(false)),
                deserialize: (bson) => Script.FromBytesUnsafe(Encoders.Hex.DecodeData(bson.AsString))
            );

            mapper.RegisterType<PartialMerkleTree>
            (
                serialize: (pmt) => Encoders.Hex.EncodeData(pmt.ToBytes()),
                deserialize: (bson) =>
                {
                    var ret = new PartialMerkleTree();
                    var bytes = Encoders.Hex.DecodeData(bson.AsString);
                    ret.ReadWrite(bytes);
                    return ret;
                }
            );

            return mapper;
        }

        public void Dispose()
        {
            this.db?.Dispose();
        }
    }
}