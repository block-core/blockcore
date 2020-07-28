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

            this.Init();
        }

        private void Init()
        {
            this.repo = new LiteRepository(this.db);

            this.dataCol = this.db.GetCollection<DataEntity>("data");
            this.identityCol = this.db.GetCollection<IdentityDocument>("identity");

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

        public IdentityDocument GetIdentity(string id)
        {
            return this.identityCol.FindById("identity/" + id);
        }

        public void SetIdentity(IdentityDocument identity)
        {
            this.identityCol.Upsert(identity);
        }

        public bool RemoveIdentity(string id)
        {
            return this.identityCol.Delete("identity/" + id);
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

            return mapper;
        }

        public void Dispose()
        {
            this.db?.Dispose();
        }
    }
}