using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Blockcore.Configuration;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Networks;
using Blockcore.Utilities;
using Dapper;
using DBreeze.Utils;
using LiteDB;
using Microsoft.Data.Sqlite;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace Blockcore.Features.Wallet.Database
{
    public class WalletStore : IWalletStore, IDisposable
    {
        private readonly SqliteConnection sqliteConnection;

        private readonly Network network;

        public WalletData WalletData { get; private set; }

        // public BsonMapper Mapper => this.db.Mapper;

        public WalletStore(Network network, Types.Wallet wallet)
        {
            this.network = network;
            this.Init(wallet);
        }

        public WalletStore(Network network, DataFolder dataFolder, Types.Wallet wallet)
        {
            var dbPath = Path.Combine(dataFolder.WalletFolderPath, $"{wallet.Name}.sqlite");

            if (!Directory.Exists(dataFolder.WalletFolderPath))
            {
                Directory.CreateDirectory(dataFolder.WalletFolderPath);
            }

            this.sqliteConnection = new SqliteConnection("Data Source=" + dbPath);

            if (!File.Exists(dbPath))
            {
                this.CreateDatabase();
            }

            this.network = network;

            this.Init(wallet);
        }

        private void Init(Types.Wallet wallet)
        {
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new HashHeightPairHandler());
            SqlMapper.AddTypeHandler(new CollectionOfuint256Handler());
            SqlMapper.AddTypeHandler(new Uint256Handler());
            SqlMapper.AddTypeHandler(new MoneyHandler());
            SqlMapper.AddTypeHandler(new OutPointHandler());
            SqlMapper.AddTypeHandler(new ScriptHandler());

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
                this.WalletData = this.sqliteConnection.QueryFirstOrDefault<WalletData>("select *, Id as Key from WalletData where Id = 'Key'");
            }

            return this.WalletData;
        }

        public void SetData(WalletData data)
        {
            var sql = "INSERT INTO 'WalletData' " +
                      "(Id, EncryptedSeed, WalletName, WalletTip, BlockLocator) " +
                      "VALUES (@Key, @EncryptedSeed, @WalletName, @WalletTip, @BlockLocator) " +
                      "ON CONFLICT(Id) DO UPDATE SET " +
                      "WalletTip = @WalletTip, BlockLocator = @BlockLocator;";

            this.sqliteConnection.Execute(sql, data);

            this.WalletData = data;
        }

        public int CountForAddress(string address)
        {
            var count = this.sqliteConnection.ExecuteScalar<int>(
                "select count(*) from 'TransactionOutputData' where Address = @address", new { address });

            return count;
        }

        public void InsertOrUpdate(TransactionOutputData item)
        {
            this.sqliteConnection.Execute("insert into 'TransactionOutputData'", item);
        }

        public IEnumerable<WalletHistoryData> GetAccountHistory(int accountIndex, bool excludeColdStake, int skip = 0, int take = 100)
        {
            // The result of this method is not guaranteed to be the length
            //  of the 'take' param. In case some of the inputs we have are
            // in the same trx they will be grouped in to a single entry.

            var historySpent = this.sqliteConnection.Query<TransactionOutputData>(
                "select * from 'TransactionOutputData' " +
                "where AccountIndex == @accountIndex" +
                "and SpendingDetailsCreationTime != null" +
                (excludeColdStake ? "IsColdCoinStake != true" : "") +
                "order by desc SpendingDetailsCreationTime" +
                "offset @skip limit @take",
                new { accountIndex, skip, take })
                .ToList();

            var historyUnspent = this.sqliteConnection.Query<TransactionOutputData>(
                    "select * from 'TransactionOutputData' " +
                    "where AccountIndex == @accountIndex" +
                    //   "and SpendingDetailsCreationTime == null" +
                    (excludeColdStake ? "IsColdCoinStake != true" : "") +
                    "order by desc CreationTime" +
                    "offset @skip limit @take",
                    new { accountIndex, skip, take })
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
            var trxs = this.sqliteConnection.Query<TransactionOutputData>(
                "select * from 'TransactionOutputData' " +
                "where Address = @address",
                new { address });

            return trxs;
        }

        public IEnumerable<TransactionOutputData> GetUnspentForAddress(string address)
        {
            var trxs = this.sqliteConnection.Query<TransactionOutputData>(
                "select * from 'TransactionOutputData' " +
                "where Address = @address" +
                "SpendingDetailsCreationTime == null",
                new { address });

            return trxs;
        }

        public WalletBalanceResult GetBalanceForAddress(string address, bool excludeColdStake)
        {
            string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND IsColdCoinStake != true " : string.Empty;

            var sql = "SELECT " +
                      "@key as Confirmed," +
                      "SUM(*.Amount) " +
                      "FROM TransactionOutputData " +
                      $"WHERE SpendingDetailsCreationTime = null AND Address = '{address}' " +
                      $"{excludeColdStakeSql}" +
                      $"GROUP BY BlockHeight != null";

            using (var res = this.sqliteConnection.ExecuteReader(sql))
            {
                var walletBalanceResult = new WalletBalanceResult();

                while (res.Read())
                {
                    if ((bool)res["Confirmed"] == false)
                        walletBalanceResult.AmountUnconfirmed = (long)res["Amount"];
                    else
                        walletBalanceResult.AmountConfirmed = (long)res["Amount"];
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

            using (var res = this.sqliteConnection.ExecuteReader(sql))
            {
                var walletBalanceResult = new WalletBalanceResult();

                while (res.Read())
                {
                    if ((bool)res["Confirmed"] == false)
                        walletBalanceResult.AmountUnconfirmed = (long)res["Amount"];
                    else
                        walletBalanceResult.AmountConfirmed = (long)res["Amount"];
                }

                return walletBalanceResult;
            }
        }

        public TransactionOutputData GetForOutput(OutPoint outPoint)
        {
            var trx = this.sqliteConnection.QueryFirstOrDefault(
                "select * from 'TransactionOutputData' where OutPoint = @outPoint", new { outPoint });
            return trx;
        }

        public bool Remove(OutPoint outPoint)
        {
            return this.sqliteConnection.QueryFirst<bool>("delete from 'TransactionOutputData' where OutPoint = @outPoint", new { outPoint });
        }

        private void CreateDatabase()
        {
            this.sqliteConnection.Execute(
               "CREATE TABLE WalletData( " +
               "Id            VARCHAR(3) NOT NULL PRIMARY KEY," +
               "EncryptedSeed VARCHAR(500) NOT NULL," +
               "WalletName    VARCHAR(100) NOT NULL," +
               "WalletTip     VARCHAR(75) NOT NULL," +
               "BlockLocator  VARCHAR(5000) NULL)");

            this.sqliteConnection.Execute(
                "CREATE TABLE TransactionOutputData(" +
                "OutPoint                                            VARCHAR(66) NOT NULL PRIMARY KEY," +
                "Address                                            VARCHAR(34) NOT NULL," +
                "Id                                                 VARCHAR(64) NOT NULL," +
                "Amount                                             INTEGER  NOT NULL," +
                "IndexInTransaction                                 BIT  NOT NULL," +
                "BlockHeight                                        INTEGER  NOT NULL," +
                "BlockHash                                          VARCHAR(64) NOT NULL," +
                "CreationTime                                       INTEGER  NOT NULL," +
                "ScriptPubKey                                       VARCHAR(50) NOT NULL," +
                "IsPropagated                                       VARCHAR(4) NOT NULL," +
                "SpendingDetailsTransactionId                       VARCHAR(64) NULL," +
                "SpendingDetailsPayments                            VARCHAR(5000) NOT NULL," +
                "SpendingDetailsBlockHeight                         INTEGER  NULL," +
                "SpendingDetailsCreationTime                        INTEGER  NULL," +
                "AccountIndex                                       BIT  NOT NULL)");

            this.sqliteConnection.Execute("CREATE INDEX 'address_index' ON 'TransactionOutputData' ('Address')");
            this.sqliteConnection.Execute("CREATE INDEX 'blockheight_index' ON 'TransactionOutputData' ('BlockHeight')");
            this.sqliteConnection.Execute("CREATE UNIQUE INDEX 'outpoint_index' ON 'TransactionOutputData' ('OutPoint')");
            this.sqliteConnection.Execute("CREATE UNIQUE INDEX 'key_index' ON 'WalletData' ('Id')");
        }

        public void Dispose()
        {
            this.sqliteConnection?.Dispose();
        }
    }

    internal abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        // Parameters are converted by Microsoft.Data.Sqlite
        public override void SetValue(IDbDataParameter parameter, T value)
            => parameter.Value = value;
    }

    internal class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
            => DateTimeOffset.Parse((string)value);
    }

    internal class ScriptHandler : SqliteTypeHandler<Script>
    {
        public override Script Parse(object value)
        {
            return Script.FromBytesUnsafe(Encoders.Hex.DecodeData((string)value));
        }

        public override void SetValue(IDbDataParameter parameter, Script value)
        {
            parameter.Value = Encoders.Hex.EncodeData(value.ToBytes(false));
        }
    }

    internal class MoneyHandler : SqliteTypeHandler<Money>
    {
        public override Money Parse(object value)
        {
            return Money.Satoshis((long)value);
        }

        public override void SetValue(IDbDataParameter parameter, Money value)
        {
            parameter.DbType = DbType.Int64;
            parameter.Value = value.Satoshi;
        }
    }

    internal class Uint256Handler : SqliteTypeHandler<uint256>
    {
        public override uint256 Parse(object value)
        {
            return uint256.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, uint256 value)
        {
            parameter.Value = value.ToString();
        }
    }

    internal class OutPointHandler : SqliteTypeHandler<OutPoint>
    {
        public override OutPoint Parse(object value)
        {
            return OutPoint.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, OutPoint value)
        {
            parameter.Value = value.ToString();
        }
    }

    internal class HashHeightPairHandler : SqliteTypeHandler<HashHeightPair>
    {
        public override HashHeightPair Parse(object value)
        {
            return HashHeightPair.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, HashHeightPair value)
        {
            parameter.Value = value.ToString();
        }
    }

    internal class CollectionOfuint256Handler : SqliteTypeHandler<ICollection<uint256>>
    {
        public override ICollection<uint256> Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            string[] items = ((string)value).Split(":");

            var uint256S = new List<uint256>();

            foreach (string item in items)
            {
                uint256S.Add(uint256.Parse(item));
            }

            return uint256S;
        }

        public override void SetValue(IDbDataParameter parameter, ICollection<uint256> value)
        {
            string values = string.Empty;

            if (value != null)
            {
                foreach (uint256 uint256 in value)
                {
                    values += uint256.ToString() + ":";
                }
            }

            parameter.Value = values;
        }
    }
}