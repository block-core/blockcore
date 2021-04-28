using System.Linq;
using System.Threading.Tasks;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Api.Controllers;
using Blockcore.Features.Wallet.Api.Models;
using Blockcore.Features.Wallet.Types;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.IntegrationTests.Wallet;
using Blockcore.Networks;
using Blockcore.Networks.Bitcoin;
using Blockcore.Tests.Common;
using FluentAssertions;
using NBitcoin;
using Xunit;

namespace Blockcore.IntegrationTests.RPC
{
    public sealed class ListAddressGroupingsTest
    {
        private const string walletName = "mywallet";
        private const string password = "password";
        private const string accountName = "account 0";

        private CoreNode miner;
        private CoreNode receiver;

        [Fact]
        [Trait("Unstable", "True")]
        public async Task ListAddressGroupingsAsync()
        {
            using (var builder = NodeBuilder.Create(this))
            {
                var network = new BitcoinRegTest();

                var nodeConfig = new NodeConfigParameters
                {
                    { "-txIndex", "1" }
                };

                this.miner = builder.CreateStratisPowNode(network, agent: "la-1-miner", configParameters: nodeConfig).WithWallet().Start();
                this.receiver = builder.CreateStratisPowNode(network, agent: "la-1-receiver", configParameters: nodeConfig).WithWallet().Start();

                // Mine blocks to get coins.
                TestHelper.MineBlocks(this.miner, 101);

                // Sync miner with receiver.
                TestHelper.ConnectAndSync(this.miner, this.receiver);

                // Send 10 coins from miner to receiver.
                SendCoins(this.miner, this.receiver, Money.Coins(10));

                // Receiver's listaddressgroupings response contains 1 array with 1 item.
                var result = await CallListAddressGroupingsAsync();
                result.Count().Should().Be(1);
                result[0].AddressGroups.First().Amount.Should().Be(Money.Coins(10));

                // Send 5 coins to miner from receiver; this will return 5 coins back to a change address on receiver.
                SendCoins(this.receiver, this.miner, Money.Coins(5));

                // Get the change address.
                var receiver_Wallet = this.receiver.FullNode.WalletManager().GetWallet(walletName);
                var firstChangeAddress = receiver_Wallet.GetAllAddresses().First(a => a.IsChangeAddress() && receiver_Wallet.walletStore.GetForAddress(a.Address).Any());

                //---------------------------------------------------
                //  Receiver's listaddressgroupings response contains 1 array with 2 items:
                //  - The initial receive address
                //  - The change address address
                //---------------------------------------------------
                result = await CallListAddressGroupingsAsync();
                result.Count().Should().Be(1);
                result[0].AddressGroups.Count().Should().Be(2);
                result[0].AddressGroups.First().Amount.Should().Be(Money.Coins(0)); // Initial receive address balance should be 0.
                result[0].AddressGroups.First(a => a.Address == firstChangeAddress.Address).Amount.Should().Be(Money.Coins((decimal)4.9999548)); // Change address balance after sending 5 coins.
                //---------------------------------------------------

                // Send 5 coins from miner to receiver's change address
                SendCoins(this.miner, this.receiver, Money.Coins(5), firstChangeAddress);

                //---------------------------------------------------
                //  Receiver's listaddressgroupings response contains 1 array with 2 items:
                //  - The initial receive address
                //  - The change address address
                //---------------------------------------------------
                result = await CallListAddressGroupingsAsync();
                result.Count().Should().Be(1);
                result[0].AddressGroups.Count().Should().Be(2);
                result[0].AddressGroups.First().Amount.Should().Be(Money.Coins(0)); // Initial receive address balance should be 0.
                result[0].AddressGroups.First(a => a.Address == firstChangeAddress.Address).Amount.Should().Be(Money.Coins((decimal)4.9999548 + 5)); // Change address balance + 5 coins.
                //---------------------------------------------------

                // Send the (full balance - 1) from receiver to miner.
                var balance = this.receiver.FullNode.WalletManager().GetSpendableTransactionsInWallet(walletName).Sum(t => t.Transaction.Amount) - Money.Coins(1);
                SendCoins(this.receiver, this.miner, balance);

                // Get the change address.
                receiver_Wallet = this.receiver.FullNode.WalletManager().GetWallet(walletName);
                var changeAddresses = receiver_Wallet.GetAllAddresses().Where(a => a.IsChangeAddress() && receiver_Wallet.walletStore.GetForAddress(a.Address).Any());
                var secondChangeAddress = receiver_Wallet.GetAllAddresses().First(a => a.IsChangeAddress() && receiver_Wallet.walletStore.GetForAddress(a.Address).Any() && a.Address != firstChangeAddress.Address);

                //---------------------------------------------------
                //  Receiver's listaddressgroupings response contains 1 array with 3 items:
                //  - The initial receive address
                //  - The change address address
                //  - The change address of sending the full balance - 1
                //---------------------------------------------------
                result = await CallListAddressGroupingsAsync();
                result.Count().Should().Be(1);
                result[0].AddressGroups.Count().Should().Be(3);
                result[0].AddressGroups.Count(a => a.Address == firstChangeAddress.Address).Should().Be(1);
                result[0].AddressGroups.Count(a => a.Address == secondChangeAddress.Address).Should().Be(1);
                result[0].AddressGroups.First(a => a.Address == secondChangeAddress.Address).Amount.Should().Be(Money.Coins((decimal)0.99992520));
                //---------------------------------------------------

                // Send 5 coins to a new unused address on the receiver's wallet.
                SendCoins(this.miner, this.receiver, Money.Coins(5));

                // Receiver's listaddressgroupings response contains 2 arrays:
                //  - Array 1 > The initial receive address
                //  - Array 1 > The change address address
                //  - Array 1 > The change address of sending the full balance - 1
                //  - Array 2 > The receive address of the new transaction
                result = await CallListAddressGroupingsAsync();
                result.Count().Should().Be(2);
                result[1].AddressGroups[0].Amount.Should().Be(Money.Coins(5));
            }
        }

        private void SendCoins(CoreNode from, CoreNode to, Money coins, HdAddress toAddress = null)
        {
            // Get a receive address.
            if (toAddress == null)
                toAddress = to.FullNode.WalletManager().GetUnusedAddress(new WalletAccountReference(walletName, accountName));

            // Send 10 coins to node.
            var transaction = from.FullNode.WalletTransactionHandler().BuildTransaction(WalletTests.CreateContext(from.FullNode.Network, new WalletAccountReference(walletName, accountName), password, toAddress.ScriptPubKey, coins, FeeType.Medium, 10));
            from.FullNode.NodeController<WalletController>().SendTransaction(new SendTransactionRequest(transaction.ToHex()));

            TestBase.WaitLoop(() => from.CreateRPCClient().GetRawMempool().Length > 0);

            // Mine the transaction.
            TestHelper.MineBlocks(this.miner, 10);
            TestBase.WaitLoop(() => TestHelper.AreNodesSynced(from, to));
            TestBase.WaitLoop(() => to.FullNode.WalletManager().GetSpendableTransactionsInWallet(walletName).Sum(x => x.Transaction.Amount) > 0);
        }

        private async Task<AddressGroupingModel[]> CallListAddressGroupingsAsync()
        {
            RPCClient client = this.receiver.CreateRPCClient();
            var response = await client.SendCommandAsync(RPCOperations.listaddressgroupings);
            var result = response.Result.ToObject<AddressGroupingModel[]>();
            client = null;

            return result;
        }
    }
}