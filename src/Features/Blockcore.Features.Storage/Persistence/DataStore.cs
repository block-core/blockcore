using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Features.Storage.Models;
using Blockcore.Utilities;
using LiteDB;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Features.Storage.Persistence
{
    public class DataStore : IDataStore, IDisposable
    {
        private LiteDatabase db;
        private LiteRepository repo;
        private readonly Network network;

        private ILiteCollection<IdentityDocument> identityCol;
        private ILiteCollection<DataEntity> dataCol;
        private ILiteCollection<HubDocument> hubCol;

        // public IdentityEntity NodeIdentity { get; private set; }

        public BsonMapper Mapper => this.db.Mapper;

        //public DataStore(Network network)
        //{
        //    BsonMapper mapper = this.Create();
        //    this.db = new LiteDatabase(new MemoryStream(), mapper: mapper);
        //    this.network = network;
        //    this.Init();
        //}

        public DataStore(DataFolder dataFolder)
        {
            var dbPath = Path.Combine(dataFolder.StoragePath, "data.db");

            if (!Directory.Exists(dataFolder.StoragePath))
            {
                Directory.CreateDirectory(dataFolder.StoragePath);
            }

            BsonMapper mapper = this.Create();
            this.db = new LiteDatabase(new ConnectionString() { Filename = dbPath }, mapper: mapper);
            //this.network = network;

            this.collections = new Dictionary<string, object>();

            this.Init();
        }

        private Dictionary<string, object> collections;

        private void Init()
        {
            this.repo = new LiteRepository(this.db);

            this.dataCol = this.db.GetCollection<DataEntity>("data");
            this.identityCol = this.db.GetCollection<IdentityDocument>("identity");
            this.hubCol = this.db.GetCollection<HubDocument>("hub");

            this.collections.Add("data", this.dataCol);
            this.collections.Add("identity", this.identityCol);
            this.collections.Add("hub", this.hubCol);

            //this.trxCol.EnsureIndex(x => x.OutPoint, true);
            //this.trxCol.EnsureIndex(x => x.Address, false);
            //this.trxCol.EnsureIndex(x => x.BlockHeight, false);

            //this.dataCol.EnsureIndex(x => x.Key, true);

            //this.WalletData = this.GetData();

            //if (this.WalletData != null)
            //{
            //    if (this.WalletData.EncryptedSeed != wallet.EncryptedSeed)
            //    {
            //        throw new WalletException("Invalid Wallet seed");
            //    }
            //}
            //else
            //{
            //    this.SetData(new WalletData
            //    {
            //        Key = "Key",
            //        EncryptedSeed = wallet.EncryptedSeed,
            //        WalletName = wallet.Name,
            //        WalletTip = new HashHeightPair(this.network.GenesisHash, 0)
            //    });
            //}
        }

        public IEnumerable<IdentityDocument> GetIdentities()
        {
            IEnumerable<IdentityDocument> identities = this.identityCol.FindAll();
            return identities;
        }

        //public IdentityDocument GetIdentity(string id)
        //{
        //    return this.identityCol.FindById("identity/" + id);
        //}

        public T GetDocumentById<T>(string collection, string id)
        {
            return ((ILiteCollection<T>)this.collections[collection]).FindById(new BsonValue($"{collection}/{id}"));
        }

        public void SetIdentity(IdentityDocument identity)
        {
            this.identityCol.Upsert(identity);
        }

        public bool RemoveIdentity(string id)
        {
            return this.identityCol.Delete("identity/" + id);
        }

        public T GetBySignature<T>(string signature, string collection)
        {
            var coll = (ILiteCollection<T>)this.collections[collection];
            T item = coll.FindOne(Query.EQ("signature.value", signature));
            return item;
        }

        public bool ExistsBySignature(string signature, string collection)
        {
            var sql = $"SELECT COUNT($.signature.value) FROM {collection} WHERE signature.value = '{signature}';";
            IBsonDataReader reader = this.db.Execute(sql);
            BsonValue item = reader.SingleOrDefault();

            return (item != null);
        }

        public IEnumerable<string> GetDocuments(string collection, IEnumerable<string> signatures)
        {
            //var skip = { (pageNumber - 1) * pageSize };

            // TODO: Can we optimize this somehow?
            BsonArray signaturesArray = new BsonArray();
            foreach (var sig in signatures)
            {
                signaturesArray.Add(sig);
            }

            // var coll = (ILiteCollection<object>)this.collections[collection];
            IEnumerable<BsonDocument> items = this.db.GetCollection(collection).Find(Query.In("signature.value", signaturesArray));

            List<string> results = new List<string>();

            foreach (BsonDocument item in items)
            {
                var json = LiteDB.JsonSerializer.Serialize(item);
                results.Add(json);
            }

            return results;

            //var coll = (ILiteCollection<T>)this.collections[collection];
            //T item = coll.FindOne(Query.EQ("signature", signature));
            //return item;

            //var sql = $"SELECT $ FROM {collection} LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize};";

            //List<string> signatures = new List<string>();

            //using (IBsonDataReader res = this.db.Execute(sql))
            //{
            //    while (res.Read())
            //    {
            //        res.Current.
            //        signatures.Add(res["signature"].AsString);
            //    }
            //}

            //return signatures;

            //ILiteCollection<BsonDocument> col = this.db.GetCollection(collection);
            //ILiteQueryable<BsonDocument> query = col.Query();
            //IEnumerable<BsonDocument> resultsPage = query.Select(x => new BsonDocument
            //{

            //    ["_id"] = x["_id"]
            //}).Limit(pageSize).Offset((pageNumber - 1) * pageSize).ToEnumerable();

            //return resultsPage;

            //string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

            //"SELECT $.Name, $.Phones[@.Type = "Mobile"] FROM customers";

            //var sql = "SELECT " +
            //            "@key as Confirmed," +
            //            "FROM " + collection + " " +
            //            $"WHERE SpendingDetails = null AND AccountIndex = {accountIndex} " +
            //            $"{excludeColdStakeSql}" +
            //            $"GROUP BY BlockHeight != null";

            //using (var res = this.db.Execute(sql))
            //{
            //    var walletBalanceResult = new WalletBalanceResult();

            //    while (res.Read())
            //    {
            //        if (res["Confirmed"] == false)
            //            walletBalanceResult.AmountUnconfirmed = res["Amount"].AsInt64;
            //        else
            //            walletBalanceResult.AmountConfirmed = res["Amount"].AsInt64;
            //    }

            //    return walletBalanceResult;
            //}

            // All query results returns an IEnumerable<T>, so you can use Linq in results
            //var results = this.identityCol
            //    .FindAll() // two indexed queries
            //    .Select(x => new { x.Name, x.Salary })
            //    .OrderBy(x => x.Name);

            //this.identityCol.Query().Select()


            //IEnumerable<string> identities = this.collections[collection];
            //return identities;
        }

        public IEnumerable<string> GetSignatures(string collection, int pageSize, int pageNumber)
        {
            var sql = $"SELECT $.signature.value FROM {collection} LIMIT {pageSize} OFFSET {(pageNumber - 1) * pageSize};";

            List<string> signatures = new List<string>();

            using (IBsonDataReader res = this.db.Execute(sql))
            {
                while (res.Read())
                {
                    signatures.Add(res["signature"].AsString);
                }
            }

            return signatures;

            //ILiteCollection<BsonDocument> col = this.db.GetCollection(collection);
            //ILiteQueryable<BsonDocument> query = col.Query();
            //IEnumerable<BsonDocument> resultsPage = query.Select(x => new BsonDocument
            //{

            //    ["_id"] = x["_id"]
            //}).Limit(pageSize).Offset((pageNumber - 1) * pageSize).ToEnumerable();

            //return resultsPage;

            //string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

            //"SELECT $.Name, $.Phones[@.Type = "Mobile"] FROM customers";

            //var sql = "SELECT " +
            //            "@key as Confirmed," +
            //            "FROM " + collection + " " +
            //            $"WHERE SpendingDetails = null AND AccountIndex = {accountIndex} " +
            //            $"{excludeColdStakeSql}" +
            //            $"GROUP BY BlockHeight != null";

            //using (var res = this.db.Execute(sql))
            //{
            //    var walletBalanceResult = new WalletBalanceResult();

            //    while (res.Read())
            //    {
            //        if (res["Confirmed"] == false)
            //            walletBalanceResult.AmountUnconfirmed = res["Amount"].AsInt64;
            //        else
            //            walletBalanceResult.AmountConfirmed = res["Amount"].AsInt64;
            //    }

            //    return walletBalanceResult;
            //}

            // All query results returns an IEnumerable<T>, so you can use Linq in results
            //var results = this.identityCol
            //    .FindAll() // two indexed queries
            //    .Select(x => new { x.Name, x.Salary })
            //    .OrderBy(x => x.Name);

            //this.identityCol.Query().Select()


            //IEnumerable<string> identities = this.collections[collection];
            //return identities;
        }


        //public int CountForAddress(string address)
        //{
        //    var count = this.trxCol.Count(Query.EQ("Address", new BsonValue(address)));
        //    return count;
        //}

        //public IEnumerable<TransactionOutputData> GetForAddress(string address)
        //{
        //    var trxs = this.trxCol.Find(Query.EQ("Address", new BsonValue(address)));
        //    return trxs;
        //}

        //public IEnumerable<TransactionOutputData> GetUnspentForAddress(string address)
        //{
        //    var trxs = this.trxCol.Find(Query.And(Query.EQ("Address", new BsonValue(address)), Query.EQ("SpendingDetails", BsonValue.Null)));
        //    return trxs;
        //}

        //public WalletBalanceResult GetBalanceForAddress(string address, bool excludeColdStake)
        //{
        //    string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

        //    var sql = "SELECT " +
        //                "@key as Confirmed," +
        //                "SUM(*.Amount) " +
        //                "FROM transactions " +
        //                $"WHERE SpendingDetails = null AND Address = '{address}' " +
        //                $"{excludeColdStakeSql}" +
        //                $"GROUP BY BlockHeight != null";

        //    using (var res = this.db.Execute(sql))
        //    {
        //        var walletBalanceResult = new WalletBalanceResult();

        //        while (res.Read())
        //        {
        //            if (res["Confirmed"] == false)
        //                walletBalanceResult.AmountUnconfirmed = res["Amount"].AsInt64;
        //            else
        //                walletBalanceResult.AmountConfirmed = res["Amount"].AsInt64;
        //        }

        //        return walletBalanceResult;
        //    }
        //}

        //public WalletBalanceResult GetBalanceForAccount(int accountIndex, bool excludeColdStake)
        //{
        //    string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

        //    var sql = "SELECT " +
        //                "@key as Confirmed," +
        //                "SUM(*.Amount) " +
        //                "FROM transactions " +
        //                $"WHERE SpendingDetails = null AND AccountIndex = {accountIndex} " +
        //                $"{excludeColdStakeSql}" +
        //                $"GROUP BY BlockHeight != null";

        //    using (var res = this.db.Execute(sql))
        //    {
        //        var walletBalanceResult = new WalletBalanceResult();

        //        while (res.Read())
        //        {
        //            if (res["Confirmed"] == false)
        //                walletBalanceResult.AmountUnconfirmed = res["Amount"].AsInt64;
        //            else
        //                walletBalanceResult.AmountConfirmed = res["Amount"].AsInt64;
        //        }

        //        return walletBalanceResult;
        //    }
        //}

        //public TransactionOutputData GetForOutput(OutPoint outPoint)
        //{
        //    var trx = this.trxCol.FindById(outPoint.ToString());
        //    return trx;
        //}

        private BsonMapper Create()
        {
            var mapper = new BsonMapper();
            mapper.UseCamelCase();

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

            // TODO: Implement handler for different versions of document to be backwards compatible.
            //mapper.RegisterType<IdentityDocument>
            //(
            //    serialize: o =>
            //    {
            //        var doc = new BsonDocument();
            //        doc["_id"] = o.Id;
            //        doc["Field1"] = o.Field1;
            //        //whatever you want to do
            //        return doc;
            //    },
            //    deserialize: doc =>
            //    {
            //        var o = new IdentityDocument();
            //        o.Id = doc["_id"];
            //        o.Field1 = doc["Field1"];
            //        //whatever you want to do
            //        return o;
            //    }
            //);

            return mapper;
        }

        public void Dispose()
        {
            this.db?.Dispose();
        }
    }
}