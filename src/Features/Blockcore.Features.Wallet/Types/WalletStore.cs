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
    public class WalletStore
    {
        private readonly ILogger logger;
        private readonly Network network;
        private LiteDatabase db;
        private LiteCollection<TrxOutput> trxCol;
        private LiteCollection<TrxOutputSlim> trxColMin; // this is experimental

        public WalletStore(Network network, DataFolder dataFolder, ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);

            string dbPath = Path.Combine(dataFolder.RootPath, "wallet.litedb");

            LiteDB.FileMode fileMode = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? LiteDB.FileMode.Exclusive : LiteDB.FileMode.Shared;
            this.db = new LiteDatabase(new ConnectionString() { Filename = dbPath, Mode = fileMode });

            this.trxCol = this.db.GetCollection<TrxOutput>("trxCol");
            this.trxColMin = this.db.GetCollection<TrxOutputSlim>("trxCol");

            this.trxCol.EnsureIndex(x => x.Utxo, true);
            this.trxCol.EnsureIndex(x => x.Address, false);
            this.trxCol.EnsureIndex(x => x.BlockHeight, false);

            this.network = network;
        }

        public int Count => this.trxCol.Count();

        public bool IsReadOnly => true;

        public void Upsert(TrxOutput item)
        {
            this.trxCol.Upsert(item);
        }

        public IEnumerable<TrxOutput> GetForAddress(string address)
        {
            var trxs = this.trxCol.Find(Query.EQ("Address", address));
            return trxs;
        }

        public IEnumerable<TrxOutputSlim> GetForAddressSlim(string address)
        {
            var trxs = this.trxColMin.Find(Query.EQ("Address", address));

            return trxs;
        }

        public TrxOutput Get(OutPoint outPoint)
        {
            var trx = this.trxCol.FindById(outPoint.ToString());
            return trx;
        }

        public bool Remove(TrxOutput item)
        {
            throw new NotImplementedException();
        }
    }
}