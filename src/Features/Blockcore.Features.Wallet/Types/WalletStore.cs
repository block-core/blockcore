using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Blockcore.Configuration;
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

        IEnumerable<TransactionData> GetForAddress(Script address);

        int CountForAddress(Script address);

        TransactionData GetForOutput(OutPoint outPoint);

        bool Remove(OutPoint outPoint);
    }

    public class WalletStore : IWalletStore
    {
        private readonly ILogger logger;
        private readonly Network network;
        private LiteDatabase db;
        private LiteCollection<TransactionData> trxCol;

        public WalletStore(Network network, DataFolder dataFolder, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);

            string dbPath = Path.Combine(dataFolder.RootPath, "wallet.litedb");

            LiteDB.FileMode fileMode = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? LiteDB.FileMode.Exclusive : LiteDB.FileMode.Shared;
            this.db = new LiteDatabase(new ConnectionString() { Filename = dbPath, Mode = fileMode });

            this.trxCol = this.db.GetCollection<TransactionData>("TransactionData");

            this.trxCol.EnsureIndex(x => x.OutPoint, true);
            this.trxCol.EnsureIndex(x => x.ScriptPubKey, false);
            this.trxCol.EnsureIndex(x => x.BlockHeight, false);

            BsonMapper.Global.Entity<TransactionData>().Id(x => x.OutPoint);

            BsonMapper.Global.RegisterType<OutPoint>
            (
                serialize: (outPoint) => outPoint.ToString(),
                deserialize: (bson) => OutPoint.Parse(bson.AsString)
            );

            BsonMapper.Global.RegisterType<uint256>
            (
                serialize: (hash) => hash.ToString(),
                deserialize: (bson) => uint256.Parse(bson.AsString)
            );

            BsonMapper.Global.RegisterType<Money>
            (
                serialize: (money) => money.Satoshi,
                deserialize: (bson) => Money.Satoshis(bson.AsInt64)
            );

            BsonMapper.Global.RegisterType<Script>
            (
                serialize: (script) => Encoders.Hex.EncodeData(script.ToBytes(false)),
                deserialize: (bson) => Script.FromBytesUnsafe(Encoders.Hex.DecodeData(bson.AsString))
            );

            BsonMapper.Global.RegisterType<PartialMerkleTree>
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

            this.network = network;
        }

        public int CountForAddress(Script address)
        {
            var req = Encoders.Hex.EncodeData(address.ToBytes(false));
            var count = this.trxCol.Count(Query.EQ("ScriptPubKey", new BsonValue(req)));
            return count;
        }

        public void InsertOrUpdate(TransactionData item)
        {
            this.trxCol.Upsert(item);
        }

        public IEnumerable<TransactionData> GetForAddress(Script address)
        {
            var req = Encoders.Hex.EncodeData(address.ToBytes(false));
            var trxs = this.trxCol.Find(Query.EQ("ScriptPubKey", new BsonValue(req)));
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
    }
}