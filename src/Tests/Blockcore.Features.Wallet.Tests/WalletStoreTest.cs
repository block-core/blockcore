using System;
using System.Collections.Generic;
using System.Globalization;
using Blockcore.Configuration;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Database;
using Blockcore.Tests.Common;
using Blockcore.Tests.Common.Logging;
using Blockcore.Utilities.JsonConverters;
using FluentAssertions;
using NBitcoin;
using Newtonsoft.Json;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class WalletStoreTest : LogsTestBase
    {
        public WalletStoreTest() : base(KnownNetworks.StratisTest)
        {
        }

        [Fact]
        public void WalletStore_Get_And_Set_And_Remove()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            var utxo = new OutPoint(new uint256(10), 1);
            var address = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();

            var trx = Create(utxo, address);

            // insert the document then fetch it and compare with source
            store.InsertOrUpdate(trx);
            var trxRes = store.GetForOutput(utxo);
            var jsonStringTrx = JsonConvert.SerializeObject(trx, new JsonSerializerSettings { Converters = new List<JsonConverter> { new MoneyJsonConverter(), new ScriptJsonConverter() } });
            var jsonStringTrxRes = JsonConvert.SerializeObject(trxRes, new JsonSerializerSettings { Converters = new List<JsonConverter> { new MoneyJsonConverter(), new ScriptJsonConverter() } });
            jsonStringTrx.Should().Be(jsonStringTrxRes);

            trx.BlockHash = null;
            trx.BlockHeight = null;
            trx.BlockIndex = null;

            // update the changed document then fetch it and compare with source
            store.InsertOrUpdate(trx);
            trxRes = store.GetForOutput(utxo);
            jsonStringTrx = JsonConvert.SerializeObject(trx, new JsonSerializerSettings { Converters = new List<JsonConverter> { new MoneyJsonConverter(), new ScriptJsonConverter() } });
            jsonStringTrxRes = JsonConvert.SerializeObject(trxRes, new JsonSerializerSettings { Converters = new List<JsonConverter> { new MoneyJsonConverter(), new ScriptJsonConverter() } });
            jsonStringTrx.Should().Be(jsonStringTrxRes);

            store.Remove(trx.OutPoint);
            var removed = store.GetForOutput(trx.OutPoint);
            removed.Should().BeNull();
        }

        [Fact]
        public void WalletStore_GetForAddress()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            var scripts = new List<string>();

            for (int indexAddress = 0; indexAddress < 3; indexAddress++)
            {
                var script = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();
                scripts.Add(script);

                for (int indexTrx = 0; indexTrx < 5; indexTrx++)
                {
                    var utxo = new OutPoint(new uint256((ulong)indexTrx), indexAddress);
                    var trx = Create(utxo, script);

                    if (indexTrx > 2)
                        trx.SpendingDetails = null;

                    store.InsertOrUpdate(trx);
                }
            }

            var findforAddress = scripts[1];
            var res = store.GetForAddress(findforAddress);

            res.Should().HaveCount(5);

            foreach (var item in res)
            {
                item.Address.Should().Be(findforAddress);
            }

            var count = store.CountForAddress(findforAddress);

            count.Should().Be(5);
        }

        [Fact]
        public void WalletStore_GetUnspentForAddress()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            var scripts = new List<string>();

            for (int indexAddress = 0; indexAddress < 3; indexAddress++)
            {
                var script = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();
                scripts.Add(script);

                for (int indexTrx = 0; indexTrx < 5; indexTrx++)
                {
                    var utxo = new OutPoint(new uint256((ulong)indexTrx), indexAddress);
                    var trx = Create(utxo, script);

                    if (indexTrx > 2)
                        trx.SpendingDetails = null;

                    store.InsertOrUpdate(trx);
                }
            }

            var findforAddress = scripts[1];
            var res = store.GetUnspentForAddress(findforAddress);

            foreach (var item in res)
            {
                item.Address.Should().Be(findforAddress);
                item.SpendingDetails.Should().BeNull();
            }
        }

        [Fact]
        public void WalletStore_GetBalanceForAddress_And_GetBalanceForAccount()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            // Create some temp data
            for (int indexAddress = 0; indexAddress < 3; indexAddress++)
            {
                var scriptInsert = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();
                for (int indexTrx = 0; indexTrx < 5; indexTrx++)
                    store.InsertOrUpdate(Create(new OutPoint(new uint256((ulong)indexTrx), indexAddress), scriptInsert));
            }

            string script = null;
            for (int accountIndex = 0; accountIndex < 2; accountIndex++)
            {
                script = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();

                TransactionOutputData trx = null;

                // spent
                trx = Create(new OutPoint(new uint256(21), accountIndex * 10), script, 2); store.InsertOrUpdate(trx);
                trx = Create(new OutPoint(new uint256(22), accountIndex * 10), script, 2); trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
                trx = Create(new OutPoint(new uint256(23), accountIndex * 10), script, 2); trx.IsColdCoinStake = true; trx.BlockHeight = null; store.InsertOrUpdate(trx);

                // cold stake unspent spend
                trx = Create(new OutPoint(new uint256(3), accountIndex * 10), script, 2); trx.IsColdCoinStake = true; trx.SpendingDetails = null; store.InsertOrUpdate(trx);
                trx = Create(new OutPoint(new uint256(4), accountIndex * 10), script, 2); trx.IsColdCoinStake = true; trx.SpendingDetails = null; store.InsertOrUpdate(trx);

                // cold stake unspent spend unconfirmed
                trx = Create(new OutPoint(new uint256(5), accountIndex * 10), script, 2); trx.IsColdCoinStake = true; trx.BlockHeight = null; trx.SpendingDetails = null; store.InsertOrUpdate(trx);
                trx = Create(new OutPoint(new uint256(6), accountIndex * 10), script, 2); trx.IsColdCoinStake = true; trx.BlockHeight = null; trx.SpendingDetails = null; store.InsertOrUpdate(trx);

                // unspent spend
                trx = Create(new OutPoint(new uint256(7), accountIndex * 10), script, 2); trx.IsColdCoinStake = false; trx.SpendingDetails = null; store.InsertOrUpdate(trx);
                trx = Create(new OutPoint(new uint256(8), accountIndex * 10), script, 2); trx.IsColdCoinStake = null; trx.SpendingDetails = null; store.InsertOrUpdate(trx);

                // unspent spend unconfirmed
                trx = Create(new OutPoint(new uint256(9), accountIndex * 10), script, 2); trx.IsColdCoinStake = false; trx.BlockHeight = null; trx.SpendingDetails = null; store.InsertOrUpdate(trx);
                trx = Create(new OutPoint(new uint256(10), accountIndex * 10), script, 2); trx.IsColdCoinStake = null; trx.BlockHeight = null; trx.SpendingDetails = null; store.InsertOrUpdate(trx);
            }

            var findforAddress = script;
            var res = store.GetBalanceForAddress(findforAddress, false);
            res.AmountConfirmed.Should().Be(20);
            res.AmountUnconfirmed.Should().Be(20);

            res = store.GetBalanceForAddress(findforAddress, true);
            res.AmountConfirmed.Should().Be(10);
            res.AmountUnconfirmed.Should().Be(10);

            res = store.GetBalanceForAccount(2, false);
            res.AmountConfirmed.Should().Be(40);
            res.AmountUnconfirmed.Should().Be(40);

            res = store.GetBalanceForAccount(2, true);
            res.AmountConfirmed.Should().Be(20);
            res.AmountUnconfirmed.Should().Be(20);
        }

        [Fact]
        public void WalletStore_GetData()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store1 = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            store1.GetData().Should().NotBeNull();
            store1.GetData().WalletTip.Height.Should().Be(0);
            store1.GetData().WalletTip.Hash.Should().Be(this.Network.GenesisHash);
            store1.GetData().WalletName.Should().Be("wallet1");
            store1.GetData().EncryptedSeed.Should().Be("EncryptedSeed1");

            var data = store1.GetData();
            data.BlockLocator = new List<uint256>() { new uint256(1), new uint256(2) };
            data.WalletTip = new Utilities.HashHeightPair(new uint256(2), 2);
            store1.SetData(data);

            store1.Dispose();

            WalletStore store2 = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            store2.GetData().WalletTip.Height.Should().Be(2);
            store2.GetData().WalletTip.Hash.Should().Be(new uint256(2));
            store2.GetData().BlockLocator.Should().HaveCount(2);

            store2.Dispose();
        }

        [Fact]
        public void WalletStore_GetAccountHistory()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            string script = null;
            script = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();

            TransactionOutputData trx = null;
            ulong index = 20;
            ulong time = 2000;
            var dt = DateTimeOffset.Now;

            // unconfirmed spent
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            // with 4 outputs
            trx = Create(new OutPoint(new uint256(index++), 00), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0001), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0002), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0003), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails.BlockHeight = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);

            // unconfirmed unspent
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            // with 4 outputs
            trx = Create(new OutPoint(new uint256(index++), 00), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0001), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0002), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0003), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.BlockHeight = null; trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);

            // confirmed spent
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            // with 4 outputs
            trx = Create(new OutPoint(new uint256(index++), 00), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0001), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0002), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0003), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);

            // confirmed unspent
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index++), 10), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            // with 4 outputs
            trx = Create(new OutPoint(new uint256(index++), 00), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0001), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = true; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0002), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);
            trx = Create(new OutPoint(new uint256(index), 0003), script, 2); trx.SpendingDetails.CreationTime = dt.AddMinutes(time--); trx.SpendingDetails = null; trx.IsColdCoinStake = false; store.InsertOrUpdate(trx);

            var res = store.GetAccountHistory(2, false);
            res.Should().HaveCount(22);
            res = store.GetAccountHistory(2, true);
            res.Should().HaveCount(15);
        }

        private TransactionOutputData Create(OutPoint outPoint, string address, int accountIndex = 0)
        {
            return new TransactionOutputData
            {
                OutPoint = outPoint,
                Address = address,
                Id = outPoint.Hash,
                Amount = 5,
                BlockHash = new uint256(50),
                BlockHeight = 5,
                BlockIndex = 2,
                AccountIndex = accountIndex,
                Hex = "TransactionHex",
                ScriptPubKey = new Script(OpcodeType.OP_0, OpcodeType.OP_1, OpcodeType.OP_3),
                MerkleProof = new PartialMerkleTree(new[] { new uint256(10), new uint256(11) }, new[] { true, false }),
                IsCoinBase = true,
                IsCoinStake = true,
                IsColdCoinStake = false,
                CreationTime = DateTimeOffset.Parse("14/06/2020 01:28:21 +01:00", new CultureInfo("nl-BE")),
                IsPropagated = false,
                SpendingDetails = new SpendingDetails
                {
                    BlockIndex = 10,
                    BlockHeight = 20,
                    Hex = "SpentTrxHex",
                    CreationTime = DateTimeOffset.Parse("14/06/2020 01:28:21 +01:00", new CultureInfo("nl-BE")),
                    IsCoinStake = true,
                    TransactionId = new uint256(100),
                    Payments = new List<PaymentDetails>
                    {
                        new PaymentDetails { Amount = 20, DestinationAddress = "DestinationAddress1", DestinationScriptPubKey = new Script(OpcodeType.OP_0, OpcodeType.OP_1) },
                        new PaymentDetails { Amount = 30, DestinationAddress = "DestinationAddress2", DestinationScriptPubKey = new Script(OpcodeType.OP_0, OpcodeType.OP_1) },
                    }
                }
            };
        }
    }
}