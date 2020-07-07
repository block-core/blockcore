using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Consensus.ValidationResults;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.Tests.Common;
using Blockcore.Tests.Common.Logging;
using FluentAssertions;
using NBitcoin;
using Xunit;

namespace Blockcore.Features.Wallet.Tests
{
    public class WalletStoreTest : LogsTestBase
    {
        [Fact]
        public void WalletStore_Upsert()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            var utxo = new OutPoint(new uint256(10), 1);
            var address = new Key().PubKey.GetAddress(this.Network).ScriptPubKey.ToString();

            var trx = Create(utxo, address);

            // insert the document then fetch it and compare with source
            store.InsertOrUpdate(trx);
            var trxRes = store.GetForOutput(utxo);
            var docTrx = store.Mapper.ToDocument(trx);
            var jsonStringTrx = LiteDB.JsonSerializer.Serialize(docTrx);
            var docTrxRes = store.Mapper.ToDocument(trxRes);
            var jsonStringTrxRes = LiteDB.JsonSerializer.Serialize(docTrxRes);
            jsonStringTrx.Should().Be(jsonStringTrxRes);

            trx.BlockHash = null;
            trx.BlockHeight = null;
            trx.BlockIndex = null;

            // update the changed document then fetch it and compare with source
            store.InsertOrUpdate(trx);
            trxRes = store.GetForOutput(utxo);
            docTrx = store.Mapper.ToDocument(trx);
            jsonStringTrx = LiteDB.JsonSerializer.Serialize(docTrx);
            docTrxRes = store.Mapper.ToDocument(trxRes);
            jsonStringTrxRes = LiteDB.JsonSerializer.Serialize(docTrxRes);
            jsonStringTrx.Should().Be(jsonStringTrxRes);
        }

        [Fact]
        public void WalletStore_FindBy_Address()
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
                    store.InsertOrUpdate(trx);
                }
            }

            var findforAddress = scripts[1];
            var res = store.GetForAddress(findforAddress);

            res.Should().HaveCount(5);

            for (int indexTrx = 0; indexTrx < 5; indexTrx++)
            {
                var utxo = new OutPoint(new uint256((ulong)indexTrx), 1);
                res.ElementAt(indexTrx).OutPoint.Should().Be(utxo);
            }
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
            data.WalletName = "wallet2";
            data.BlockLocator = new List<uint256>() { new uint256(1), new uint256(2) };
            data.WalletTip = new Utilities.HashHeightPair(new uint256(2), 2);
            store1.SetData(data);

            store1.Dispose();

            WalletStore store2 = new WalletStore(this.Network, dataFolder, new Types.Wallet { Name = "wallet1", EncryptedSeed = "EncryptedSeed1" });

            store2.GetData().WalletTip.Height.Should().Be(2);
            store2.GetData().WalletTip.Hash.Should().Be(new uint256(2));
            store2.GetData().WalletName.Should().Be("wallet2");
            store2.GetData().BlockLocator.Should().HaveCount(2);

            store2.Dispose();
        }

        private TransactionOutputData Create(OutPoint outPoint, string address)
        {
            return new TransactionOutputData
            {
                OutPoint = outPoint,
                Address = address,
                Amount = 5,
                BlockHash = new uint256(50),
                BlockHeight = 5,
                BlockIndex = 2,
                Hex = "TransactionHex",
                ScriptPubKey = new Script(OpcodeType.OP_0, OpcodeType.OP_1, OpcodeType.OP_3),
                MerkleProof = new PartialMerkleTree(new[] { new uint256(10), new uint256(11) }, new[] { true, false }),
                IsCoinBase = true,
                IsCoinStake = true,
                IsColdCoinStake = false,
                CreationTime = DateTimeOffset.Parse("14/06/2020 01:28:21 +01:00"),
                IsPropagated = false,
                SpendingDetails = new SpendingDetails
                {
                    BlockIndex = 10,
                    BlockHeight = 20,
                    Hex = "SpentTrxHex",
                    CreationTime = DateTimeOffset.Parse("14/06/2020 01:28:21 +01:00"),
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