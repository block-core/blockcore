using System;
using System.Collections.Generic;
using System.IO;
using Blockcore.Configuration;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Features.Wallet.Types;
using Blockcore.Utilities;
using LiteDB;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Features.Wallet.Database
{
    public class WalletStore : IWalletStore, IDisposable
    {
        private LiteDatabase db;
        private readonly Network network;
        private ILiteCollection<WalletData> dataCol;
        private ILiteCollection<TransactionOutputData> trxCol;
        private string dbPath;

        public WalletData WalletData { get; private set; }

        public BsonMapper Mapper => this.db.Mapper;

        public WalletStore(Network network, DataFolder dataFolder, Types.Wallet wallet)
        {
            this.dbPath = Path.Combine(dataFolder.WalletFolderPath, $"{wallet.Name}.db");

            if (!Directory.Exists(dataFolder.WalletFolderPath))
            {
                Directory.CreateDirectory(dataFolder.WalletFolderPath);
            }

            BsonMapper mapper = this.Create();
            this.db = new LiteDatabase(new ConnectionString() { Filename = this.dbPath }, mapper: mapper);

            this.trxCol = this.db.GetCollection<TransactionOutputData>("transactions");
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

        public void InsertOrUpdate(TransactionOutputData item)
        {
            this.trxCol.Upsert(item);
        }

        public IEnumerable<TransactionOutputData> GetForAddress(string address)
        {
            var trxs = this.trxCol.Find(Query.EQ("Address", new BsonValue(address)));
            return trxs;
        }

        public IEnumerable<TransactionOutputData> GetUnspentForAddress(string address)
        {
            var trxs = this.trxCol.Find(Query.And(Query.EQ("Address", new BsonValue(address)), Query.EQ("SpendingDetails", BsonValue.Null)));
            return trxs;
        }

        public TransactionOutputData GetForOutput(OutPoint outPoint)
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