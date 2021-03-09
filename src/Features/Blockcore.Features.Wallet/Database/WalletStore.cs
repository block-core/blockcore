using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Networks;
using Blockcore.Utilities;
using Blockcore.Utilities.JsonConverters;
using Dapper;
using DBreeze.Utils;
using Microsoft.Data.Sqlite;
using NBitcoin;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Database
{
    public class WalletStore : IWalletStore, IDisposable
    {
        private const int WalletVersion = 1;

        /// <summary>
        /// A connection used only when the data store is in memory only.
        /// SQLite in-memory mode will keep the db as long as there is one connection open.
        /// https://github.com/dotnet/docs/blob/master/samples/snippets/standard/data/sqlite/InMemorySample/Program.cs
        /// /// </summary>
        private readonly SqliteConnection inmemorySqliteConnection;

        private readonly Network network;

        private readonly string dbPath;
        private readonly string dbConnection;

        public WalletData WalletData { get; private set; }

        /// <summary>
        /// Constructor that initiates an in-memory sql engine instance and is used only for tests.
        /// </summary>
        public WalletStore(Network network, Types.Wallet wallet)
        {
            var tmpconn = Guid.NewGuid().ToString();

            this.dbConnection = $"Data Source={tmpconn};Mode=Memory;Cache=Shared";
            this.inmemorySqliteConnection = new SqliteConnection(this.dbConnection);
            this.inmemorySqliteConnection.Open();

            this.CreateDatabase();

            this.network = network;
            this.Init(wallet);
        }

        public WalletStore(Network network, DataFolder dataFolder, Types.Wallet wallet)
        {
            this.dbPath = Path.Combine(dataFolder.WalletFolderPath, $"{wallet.Name}.db");
            this.dbConnection = "Data Source=" + this.dbPath;

            if (!Directory.Exists(dataFolder.WalletFolderPath))
            {
                Directory.CreateDirectory(dataFolder.WalletFolderPath);
            }

            if (!File.Exists(this.dbPath))
            {
                this.CreateDatabase();
            }
            else
            {
                // Attempt to access the user version, this will crash if the loaded database is V5 and we use V4 packages.
                try
                {
                    var walletVersion = -1;

                    using (var conn = this.GetDbConnection())
                    {
                        walletVersion = conn.QueryFirst<int>("SELECT WalletVersion FROM WalletData");
                    }

                    if (walletVersion != WalletVersion)
                    {
                        this.UpgradeDatabase(walletVersion);
                    }
                }
                catch (Microsoft.Data.Sqlite.SqliteException sqex)
                {
                    // Errror that indicates that the file being opened does not appear to be an SQLite database file.
                    if (sqex.SqliteErrorCode != 26)
                        throw;

                    // This will make a backup copy of the old litedbv5 (or v4) databases.
                    // The reason the code base moved to use sqlite instead of litedb is because litedbv5 is not
                    // properly maintained anymore and has a critical hard to reproduce errors that happen randomly.
                    var dbBackupPath = Path.Combine(dataFolder.WalletFolderPath, $"{wallet.Name}.error.db");

                    // Move the problematic database file, which might be a V5 database.
                    File.Move(this.dbPath, dbBackupPath);

                    this.CreateDatabase();
                }
            }

            this.network = network;

            this.Init(wallet);
        }

        protected SqliteConnection GetDbConnection()
        {
            return new SqliteConnection(this.dbConnection);
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
            SqlMapper.AddTypeHandler(new CollectionOfPaymentDetailsHandler());
            SqlMapper.AddTypeHandler(new PartialMerkleTreeHandler());

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
                    WalletTip = new HashHeightPair(this.network.GenesisHash, 0),
                    WalletVersion = WalletVersion
                });
            }
        }

        public WalletData GetData()
        {
            if (this.WalletData == null)
            {
                using var conn = this.GetDbConnection();
                this.WalletData = conn.QueryFirstOrDefault<WalletData>("SELECT *, Id AS Key FROM WalletData WHERE Id = 'Key'");
            }

            return this.WalletData;
        }

        public void SetData(WalletData data)
        {
            var sql = @$"INSERT INTO WalletData
                      (Id, EncryptedSeed, WalletName, WalletTip, WalletVersion, BlockLocator)
                      VALUES (@Key, @EncryptedSeed, @WalletName, @WalletTip, @WalletVersion, @BlockLocator)
                      ON CONFLICT(Id) DO UPDATE SET
                      EncryptedSeed = @EncryptedSeed, WalletTip = @WalletTip, BlockLocator = @BlockLocator;";

            using var conn = this.GetDbConnection();
            conn.Execute(sql, data);

            this.WalletData = data;
        }

        public void InsertOrUpdate(TransactionOutputData item)
        {
            TransactionData insert = this.Convert(item);

            var sql = @$"INSERT INTO TransactionData
                      (OutPoint, Address, Id, Amount, IndexInTransaction, BlockHeight, BlockHash, BlockIndex, CreationTime, ScriptPubKey, IsPropagated, IsCoinBase, IsCoinStake, IsColdCoinStake, AccountIndex, MerkleProof, Hex, SpendingDetailsTransactionId, SpendingDetailsBlockHeight, SpendingDetailsBlockIndex, SpendingDetailsIsCoinStake, SpendingDetailsCreationTime, SpendingDetailsPayments, SpendingDetailsHex)
                      VALUES (@OutPoint, @Address, @Id, @Amount, @IndexInTransaction, @BlockHeight, @BlockHash, @BlockIndex, @CreationTime, @ScriptPubKey, @IsPropagated, @IsCoinBase, @IsCoinStake, @IsColdCoinStake, @AccountIndex, @MerkleProof, @Hex, @SpendingDetailsTransactionId, @SpendingDetailsBlockHeight, @SpendingDetailsBlockIndex, @SpendingDetailsIsCoinStake, @SpendingDetailsCreationTime, @SpendingDetailsPayments, @SpendingDetailsHex)
                      ON CONFLICT(OutPoint) DO UPDATE SET
                      IndexInTransaction = @IndexInTransaction, BlockHeight = @BlockHeight, BlockHash = @BlockHash, BlockIndex = @BlockIndex, CreationTime = @CreationTime, IsPropagated = @IsPropagated, IsColdCoinStake = @IsColdCoinStake, AccountIndex = @AccountIndex, MerkleProof = @MerkleProof, Hex = @Hex, SpendingDetailsTransactionId = @SpendingDetailsTransactionId, SpendingDetailsBlockHeight = @SpendingDetailsBlockHeight, SpendingDetailsBlockIndex = @SpendingDetailsBlockIndex, SpendingDetailsIsCoinStake = @SpendingDetailsIsCoinStake, SpendingDetailsCreationTime = @SpendingDetailsCreationTime, SpendingDetailsPayments = @SpendingDetailsPayments, SpendingDetailsHex = @SpendingDetailsHex;";

            using var conn = this.GetDbConnection();
            conn.Execute(sql, insert);
        }

        public int CountForAddress(string address)
        {
            using var conn = this.GetDbConnection();
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM TransactionData WHERE Address = @address", new { address });

            return count;
        }

        public IEnumerable<WalletHistoryData> GetAccountHistory(int accountIndex, bool excludeColdStake, int skip = 0, int take = 100)
        {
            // The result of this method is not guaranteed to be the length
            //  of the 'take' param. In case some of the inputs we have are
            // in the same trx they will be grouped in to a single entry.
            using var conn = this.GetDbConnection();
            conn.Open();

            var sql = @$"SELECT * FROM TransactionData
                      WHERE AccountIndex == @accountIndex
                      AND SpendingDetailsTransactionId IS NOT NULL
                      {(excludeColdStake ? "AND (IsColdCoinStake = false OR IsColdCoinStake is null) " : "")}
                      ORDER BY SpendingDetailsCreationTime DESC
                      LIMIT @take OFFSET @skip ";

            var historySpentResult = conn.Query<TransactionData>(sql, new { accountIndex, skip, take }).ToList();
            var historySpent = historySpentResult.Select(this.Convert);

            sql = @$"SELECT * FROM TransactionData
                  WHERE AccountIndex == @accountIndex
                  {(excludeColdStake ? "AND (IsColdCoinStake = false OR IsColdCoinStake is null) " : "")}
                  ORDER BY CreationTime DESC
                  LIMIT @take OFFSET @skip";

            var historyUnspentResult = conn.Query<TransactionData>(sql, new { accountIndex, skip, take }).ToList();

            conn.Close();

            var historyUnspent = historyUnspentResult.Select(this.Convert);

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
            using var conn = this.GetDbConnection();
            var trxs = conn.Query<TransactionData>(
                "SELECT * FROM TransactionData " +
                "WHERE Address = @address",
                new { address });

            return trxs.Select(this.Convert);
        }

        public IEnumerable<TransactionOutputData> GetUnspentForAddress(string address)
        {
            using var conn = this.GetDbConnection();
            var trxs = conn.Query<TransactionData>(
                "SELECT * FROM 'TransactionData' " +
                "WHERE Address = @address " +
                "AND SpendingDetailsTransactionId IS NULL",
                new { address });

            return trxs.Select(this.Convert);
        }

        public WalletBalanceResult GetBalanceForAddress(string address, bool excludeColdStake)
        {
            string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND (IsColdCoinStake = false OR IsColdCoinStake IS NULL) " : string.Empty;

            var sql = @$"SELECT
                        BlockHeight as Confirmed,
                        SUM(Amount) as Total
                        FROM TransactionData
                        WHERE SpendingDetailsTransactionId IS NULL AND Address = @address
                        {excludeColdStakeSql}
                        GROUP BY BlockHeight IS NOT NULL";

            using var conn = this.GetDbConnection();
            var result = conn.Query(sql, new { address });

            var walletBalanceResult = new WalletBalanceResult();

            foreach (dynamic item in result)
            {
                if (item.Confirmed == null) walletBalanceResult.AmountUnconfirmed = (long)item.Total;
                else walletBalanceResult.AmountConfirmed = (long)item.Total;
            }

            return walletBalanceResult;
        }

        public WalletBalanceResult GetBalanceForAccount(int accountIndex, bool excludeColdStake)
        {
            string excludeColdStakeSql = excludeColdStake && this.network.Consensus.IsProofOfStake ? "AND (IsColdCoinStake = false OR IsColdCoinStake IS NULL) " : string.Empty;

            var sql = @$"SELECT
                      BlockHeight as Confirmed,
                      SUM(Amount) as Total
                      FROM TransactionData
                      WHERE SpendingDetailsTransactionId IS NULL AND AccountIndex = @accountIndex
                      {excludeColdStakeSql}
                      GROUP BY BlockHeight IS NOT NULL";

            using var conn = this.GetDbConnection();
            var result = conn.Query(sql, new { accountIndex });

            var walletBalanceResult = new WalletBalanceResult();

            foreach (dynamic item in result)
            {
                if (item.Confirmed == null) walletBalanceResult.AmountUnconfirmed = (long)item.Total;
                else walletBalanceResult.AmountConfirmed = (long)item.Total;
            }

            return walletBalanceResult;
        }

        public TransactionOutputData GetForOutput(OutPoint outPoint)
        {
            TransactionData trx = null;

            using var conn = this.GetDbConnection();
            trx = conn.QueryFirstOrDefault<TransactionData>("SELECT * FROM TransactionData WHERE OutPoint = @outPoint", new { outPoint });

            if (trx == null)
            {
                return null;
            }

            TransactionOutputData ret = this.Convert(trx);

            return ret;
        }

        public bool Remove(OutPoint outPoint)
        {
            using var conn = this.GetDbConnection();
            var ret = conn.ExecuteScalar<int>("DELETE FROM TransactionData WHERE OutPoint = @outPoint", new { outPoint });
            return ret > 0;
        }

        private void UpgradeDatabase(int oldVersion)
        {
            // Here can come code to upgrade the db from old to current version.
        }

        private void CreateDatabase()
        {
            using (var conn = GetDbConnection())
            {
                conn.Open();

                conn.Execute(
                   @$"CREATE TABLE WalletData(
               Id            VARCHAR(3) NOT NULL PRIMARY KEY,
               EncryptedSeed VARCHAR(500) NULL,
               WalletName    VARCHAR(100) NOT NULL,
               WalletTip     VARCHAR(75) NOT NULL,
               WalletVersion INTEGER NOT NULL,
               BlockLocator  TEXT NULL)");

                conn.Execute(
                    @$"CREATE TABLE TransactionData(
                OutPoint                                           VARCHAR(66) NOT NULL PRIMARY KEY,
                Address                                            VARCHAR(34) NOT NULL,
                Id                                                 VARCHAR(64) NOT NULL,
                Amount                                             INTEGER  NOT NULL,
                IndexInTransaction                                 INTEGER  NOT NULL,
                BlockHeight                                        INTEGER  NULL,
                BlockHash                                          VARCHAR(64) NULL,
                BlockIndex                                         INTEGER NULL,
                CreationTime                                       INTEGER  NOT NULL,
                ScriptPubKey                                       VARCHAR(100) NOT NULL,
                IsPropagated                                       INTEGER  NULL,
                IsCoinBase                                         INTEGER  NULL,
                IsCoinStake                                        INTEGER  NULL,
                IsColdCoinStake                                    INTEGER  NULL,
                AccountIndex                                       INTEGER  NOT NULL,
                MerkleProof                                        TEXT NULL,
                Hex                                                TEXT NULL,
                SpendingDetailsTransactionId                       VARCHAR(64) NULL,
                SpendingDetailsBlockHeight                         INTEGER  NULL,
                SpendingDetailsBlockIndex                          INTEGER  NULL,
                SpendingDetailsIsCoinStake                         INTEGER  NULL,
                SpendingDetailsCreationTime                        INTEGER  NULL,
                SpendingDetailsPayments                            TEXT NULL,
                SpendingDetailsHex                                 TEXT NULL)");

                conn.Execute("CREATE INDEX 'address_index' ON 'TransactionData' ('Address')");
                conn.Execute("CREATE INDEX 'blockheight_index' ON 'TransactionData' ('BlockHeight')");
                conn.Execute("CREATE UNIQUE INDEX 'outpoint_index' ON 'TransactionData' ('OutPoint')");
                conn.Execute("CREATE UNIQUE INDEX 'key_index' ON 'WalletData' ('Id')");

                conn.Close();
            }
        }

        public void Dispose()
        {
            this.inmemorySqliteConnection?.Dispose();
        }

        private TransactionData Convert(TransactionOutputData source)
        {
            var target = new TransactionData
            {
                OutPoint = source.OutPoint,
                Address = source.Address,
                Id = source.Id ?? source.OutPoint.Hash,
                Amount = source.Amount ?? 0,
                IndexInTransaction = source.Index,
                BlockHeight = source.BlockHeight,
                BlockHash = source.BlockHash,
                BlockIndex = source.BlockIndex,
                CreationTime = source.CreationTime,
                ScriptPubKey = source.ScriptPubKey ?? Script.Empty,
                IsPropagated = source.IsPropagated,
                IsCoinBase = source.IsCoinBase,
                IsCoinStake = source.IsCoinStake,
                IsColdCoinStake = source.IsColdCoinStake,
                AccountIndex = source.AccountIndex,
                MerkleProof = source.MerkleProof,
                Hex = source.Hex,
                SpendingDetailsTransactionId = source.SpendingDetails?.TransactionId,
                SpendingDetailsBlockHeight = source.SpendingDetails?.BlockHeight,
                SpendingDetailsBlockIndex = source.SpendingDetails?.BlockIndex,
                SpendingDetailsIsCoinStake = source.SpendingDetails?.IsCoinStake,
                SpendingDetailsCreationTime = source.SpendingDetails?.CreationTime,
                SpendingDetailsPayments = source.SpendingDetails?.Payments,
                SpendingDetailsHex = source.SpendingDetails?.Hex
            };

            return target;
        }

        private TransactionOutputData Convert(TransactionData source)
        {
            var target = new TransactionOutputData
            {
                OutPoint = source.OutPoint,
                Address = source.Address,
                Id = source.Id,
                Amount = source.Amount,
                Index = source.IndexInTransaction,
                BlockHeight = source.BlockHeight,
                BlockIndex = source.BlockIndex,
                BlockHash = source.BlockHash,
                CreationTime = source.CreationTime,
                ScriptPubKey = source.ScriptPubKey,
                IsPropagated = source.IsPropagated,
                IsCoinBase = source.IsCoinBase,
                IsCoinStake = source.IsCoinStake,
                IsColdCoinStake = source.IsColdCoinStake,
                AccountIndex = source.AccountIndex,
                MerkleProof = source.MerkleProof,
                Hex = source.Hex
            };

            if (source.SpendingDetailsTransactionId != null)
            {
                target.SpendingDetails = new SpendingDetails
                {
                    TransactionId = source.SpendingDetailsTransactionId,
                    BlockHeight = source.SpendingDetailsBlockHeight,
                    BlockIndex = source.SpendingDetailsBlockIndex,
                    IsCoinStake = source.SpendingDetailsIsCoinStake,
                    CreationTime = source.SpendingDetailsCreationTime.Value,
                    Payments = source.SpendingDetailsPayments,
                    Hex = source.SpendingDetailsHex
                };
            }

            return target;
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

    internal class PartialMerkleTreeHandler : SqliteTypeHandler<PartialMerkleTree>
    {
        public override PartialMerkleTree Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            var ret = new PartialMerkleTree();
            var bytes = Encoders.Hex.DecodeData((string)value);
            ret.ReadWrite(bytes);

            return ret;
        }

        public override void SetValue(IDbDataParameter parameter, PartialMerkleTree value)
        {
            string values = string.Empty;

            if (value != null)
            {
                values = Encoders.Hex.EncodeData(value.ToBytes());
            }

            parameter.Value = values;
        }
    }

    internal class CollectionOfuint256Handler : SqliteTypeHandler<ICollection<uint256>>
    {
        private static readonly JsonSerializerSettings Converters = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new UInt256JsonConverter() }
        };

        public override ICollection<uint256> Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            var res = JsonConvert.DeserializeObject<ICollection<uint256>>((string)value, Converters);

            return res;
        }

        public override void SetValue(IDbDataParameter parameter, ICollection<uint256> value)
        {
            string values = string.Empty;

            if (value != null)
            {
                values = JsonConvert.SerializeObject(value, Converters);
            }

            parameter.Value = values;
        }
    }

    internal class CollectionOfPaymentDetailsHandler : SqliteTypeHandler<ICollection<PaymentDetails>>
    {
        private static readonly JsonSerializerSettings Converters = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new MoneyJsonConverter(), new ScriptJsonConverter() }
        };

        public override ICollection<PaymentDetails> Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            var res = JsonConvert.DeserializeObject<ICollection<PaymentDetails>>((string)value, Converters);

            return res;
        }

        public override void SetValue(IDbDataParameter parameter, ICollection<PaymentDetails> value)
        {
            string values = string.Empty;

            if (value != null)
            {
                values = JsonConvert.SerializeObject(value, Converters);
            }

            parameter.Value = values;
        }
    }
}