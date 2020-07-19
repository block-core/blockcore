using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Utilities;
using DBreeze.Utils;
using LiteDB;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Features.Wallet.Database
{
    public class WalletStore : IWalletStore, IDisposable
    {
        private LiteDatabase db;
        private LiteRepository repo;
        private readonly Network network;
        private ILiteCollection<WalletData> dataCol;
        private ILiteCollection<TransactionOutputData> trxCol;

        public WalletData WalletData { get; private set; }

        public BsonMapper Mapper => this.db.Mapper;

        public WalletStore(Network network, Types.Wallet wallet)
        {
            BsonMapper mapper = this.Create();
            this.db = new LiteDatabase(new MemoryStream(), mapper: mapper);
            this.network = network;
            this.Init(wallet);
        }

        public WalletStore(Network network, DataFolder dataFolder, Types.Wallet wallet)
        {
            var dbPath = Path.Combine(dataFolder.WalletFolderPath, $"{wallet.Name}.db");

            if (!Directory.Exists(dataFolder.WalletFolderPath))
            {
                Directory.CreateDirectory(dataFolder.WalletFolderPath);
            }

            BsonMapper mapper = this.Create();
            this.db = new LiteDatabase(new ConnectionString() { Filename = dbPath }, mapper: mapper);
            this.network = network;

            this.Init(wallet);
        }

        private void Init(Types.Wallet wallet)
        {
            this.repo = new LiteRepository(this.db);

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
                    WalletTip = new HashHeightPair(this.network.GenesisHash, 0)
                });
            }
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

        public IEnumerable<WalletHistoryData> GetAccountHistory(int accountIndex, bool excludeColdStake, int skip = 0, int take = 100)
        {
            // The result of this method is not guaranteed to be the length
            //  of the 'take' param. In case some of the inputs we have are
            // in the same trx they will be grouped in to a single entry.

            var historySpent = this.trxCol.Query()
              .Where(x => x.AccountIndex == accountIndex)
              .Where(x => x.SpendingDetails != null)
              .Where(x => excludeColdStake ? (x.IsColdCoinStake != true) : true)
              .OrderByDescending(x => x.SpendingDetails.CreationTime)
              .Skip(skip)
              .Limit(take)
              .ToList();

            var historyUnspent = this.trxCol.Query()
                .Where(x => x.AccountIndex == accountIndex)
                .Where(x => excludeColdStake ? (x.IsColdCoinStake != true) : true)
                .OrderByDescending(x => x.CreationTime)
                .Skip(skip)
                .Limit(take)
                .ToList();

            var items = new List<WalletHistoryData>();

            items.AddRange(historySpent
                .GroupBy(g => g.SpendingDetails.TransactionId)
                   .Select(s =>
                   {
                       var x = s.First();

                       return new WalletHistoryData
                       {
                           IsSent = true,
                           SentTo = x.SpendingDetails.TransactionId,
                           IsCoinStake = x.SpendingDetails.IsCoinStake,
                           CreationTime = x.SpendingDetails.CreationTime,
                           BlockHeight = x.SpendingDetails.BlockHeight,
                           BlockIndex = x.SpendingDetails.BlockIndex,
                           SentPayments = x.SpendingDetails.Payments?.Select(p => new WalletHistoryPaymentData
                           {
                               Amount = p.Amount,
                               PayToSelf = p.PayToSelf,
                               DestinationAddress = p.DestinationAddress
                           }).ToList(),

                           // when spent the amount represents the
                           // input that was spent not the output
                           Amount = x.Amount
                       };
                   }));

            items.AddRange(historyUnspent
                .GroupBy(g => g.Id)
                .Select(s =>
                {
                    var x = s.First();

                    var ret = new WalletHistoryData
                    {
                        IsSent = false,
                        OutPoint = x.OutPoint,
                        BlockHeight = x.BlockHeight,
                        BlockIndex = x.BlockIndex,
                        IsCoinStake = x.IsCoinStake,
                        CreationTime = x.CreationTime,
                        ScriptPubKey = x.ScriptPubKey,
                        Address = x.Address,
                        Amount = x.Amount,
                        IsCoinBase = x.IsCoinBase,
                        IsColdCoinStake = x.IsColdCoinStake,
                    };

                    if (s.Count() > 1)
                    {
                        ret.Amount = s.Sum(b => b.Amount);
                        ret.ReceivedOutputs = s.Select(b => new WalletHistoryData
                        {
                            IsSent = false,
                            OutPoint = b.OutPoint,
                            BlockHeight = b.BlockHeight,
                            BlockIndex = b.BlockIndex,
                            IsCoinStake = b.IsCoinStake,
                            CreationTime = b.CreationTime,
                            ScriptPubKey = b.ScriptPubKey,
                            Address = b.Address,
                            Amount = b.Amount,
                            IsCoinBase = b.IsCoinBase,
                            IsColdCoinStake = b.IsColdCoinStake,
                        }).ToList();
                    }

                    return ret;
                }));

            return items.OrderByDescending(x => x.CreationTime).ThenBy(x => x.BlockIndex);
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

        public WalletBalanceResult GetBalanceForAddress(string address, bool excludeColdStake)
        {
            string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

            var sql = "SELECT " +
                        "@key as Confirmed," +
                        "SUM(*.Amount) " +
                        "FROM transactions " +
                        $"WHERE SpendingDetails = null AND Address = '{address}' " +
                        $"{excludeColdStakeSql}" +
                        $"GROUP BY BlockHeight != null";

            using (var res = this.db.Execute(sql))
            {
                var walletBalanceResult = new WalletBalanceResult();

                while (res.Read())
                {
                    if (res["Confirmed"] == false)
                        walletBalanceResult.AmountUnconfirmed = res["Amount"].AsInt64;
                    else
                        walletBalanceResult.AmountConfirmed = res["Amount"].AsInt64;
                }

                return walletBalanceResult;
            }
        }

        public WalletBalanceResult GetBalanceForAccount(int accountIndex, bool excludeColdStake)
        {
            string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

            var sql = "SELECT " +
                        "@key as Confirmed," +
                        "SUM(*.Amount) " +
                        "FROM transactions " +
                        $"WHERE SpendingDetails = null AND AccountIndex = {accountIndex} " +
                        $"{excludeColdStakeSql}" +
                        $"GROUP BY BlockHeight != null";

            using (var res = this.db.Execute(sql))
            {
                var walletBalanceResult = new WalletBalanceResult();

                while (res.Read())
                {
                    if (res["Confirmed"] == false)
                        walletBalanceResult.AmountUnconfirmed = res["Amount"].AsInt64;
                    else
                        walletBalanceResult.AmountConfirmed = res["Amount"].AsInt64;
                }

                return walletBalanceResult;
            }
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