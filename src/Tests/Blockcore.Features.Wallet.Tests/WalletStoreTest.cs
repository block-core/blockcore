using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Configuration;
using Blockcore.Consensus.ValidationResults;
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

            WalletStore store = new WalletStore(this.Network, dataFolder, this.LoggerFactory.Object);

            var utxo = new OutPoint(new uint256(10), 1);
            var address = new Key().PubKey.GetAddress(this.Network).ScriptPubKey;

            var trx = Create(utxo, address);

            // insert the document then fetch it and compare with source
            store.Upsert(trx);
            var trxRes = store.Get(utxo);
            var docTrx = LiteDB.BsonMapper.Global.ToDocument(trx);
            var jsonStringTrx = LiteDB.JsonSerializer.Serialize(docTrx, false, true);
            var docTrxRes = LiteDB.BsonMapper.Global.ToDocument(trxRes);
            var jsonStringTrxRes = LiteDB.JsonSerializer.Serialize(docTrxRes, false, true);
            jsonStringTrx.Should().Be(jsonStringTrxRes);

            trx.BlockHash = null;
            trx.BlockHeight = null;
            trx.BlockIndex = null;

            // update the changed document then fetch it and compare with source
            store.Upsert(trx);
            trxRes = store.Get(utxo);
            docTrx = LiteDB.BsonMapper.Global.ToDocument(trx);
            jsonStringTrx = LiteDB.JsonSerializer.Serialize(docTrx, false, true);
            docTrxRes = LiteDB.BsonMapper.Global.ToDocument(trxRes);
            jsonStringTrxRes = LiteDB.JsonSerializer.Serialize(docTrxRes, false, true);
            jsonStringTrx.Should().Be(jsonStringTrxRes);
        }

        [Fact]
        public void WalletStore_FindBy_Address()
        {
            DataFolder dataFolder = CreateDataFolder(this);

            WalletStore store = new WalletStore(this.Network, dataFolder, this.LoggerFactory.Object);

            var scripts = new List<Script>();

            for (int indexAddress = 0; indexAddress < 3; indexAddress++)
            {
                var script = new Key().PubKey.GetAddress(this.Network).ScriptPubKey;
                scripts.Add(script);

                for (int indexTrx = 0; indexTrx < 5; indexTrx++)
                {
                    var utxo = new OutPoint(new uint256((ulong)indexTrx), indexAddress);
                    var trx = Create(utxo, script);
                    store.Upsert(trx);
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

        private TransactionData Create(OutPoint outPoint, Script script)
        {
            return new TransactionData
            {
                OutPoint = outPoint,
                Amount = 5,
                BlockHash = new uint256(50),
                BlockHeight = 5,
                BlockIndex = 2,
                Hex = "TransactionHex",
                ScriptPubKey = script,
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