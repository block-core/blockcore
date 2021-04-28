using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.Miner.Staking;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.Networks;
using Blockcore.Networks.Stratis;
using Blockcore.Tests.Common;
using FluentAssertions;
using NBitcoin;
using Xunit;

namespace Blockcore.IntegrationTests.Miners
{
    public sealed class ProofOfStakeMintCoinsTest
    {
        private readonly HashSet<uint256> transactionsBeforeStaking = new HashSet<uint256>();
        private readonly ConcurrentDictionary<uint256, TransactionOutputData> transactionLookup = new ConcurrentDictionary<uint256, TransactionOutputData>();

        [Fact]
        public void Staking_Wallet_Can_Mint_New_Coins()
        {
            using (var builder = NodeBuilder.Create(this))
            {
                var configParameters = new NodeConfigParameters { { "savetrxhex", "true" } };
                var network = new StratisOverrideRegTest();

                var minerA = builder.CreateStratisPosNode(network, "stake-1-minerA", configParameters: configParameters).OverrideDateTimeProvider().WithWallet().Start();

                var addressUsed = TestHelper.MineBlocks(minerA, (int)network.Consensus.PremineHeight).AddressUsed;
                var wallet = minerA.FullNode.WalletManager().Wallets.Single(w => w.Name == "mywallet");
                var allTrx = wallet.walletStore.GetForAddress(addressUsed.Address);

                // Since the pre-mine will not be immediately spendable, the transactions have to be counted directly from the address.
                allTrx.Count().Should().Be((int)network.Consensus.PremineHeight);

                allTrx.Sum(s => s.Amount).Should().Be(network.Consensus.PremineReward + network.Consensus.ProofOfWorkReward);
                var balance = minerA.FullNode.WalletManager().GetAddressBalance(addressUsed.Address);
                balance.AmountConfirmed.Should().Be(network.Consensus.PremineReward + network.Consensus.ProofOfWorkReward);

                // Mine blocks to maturity
                TestHelper.MineBlocks(minerA, (int)network.Consensus.CoinbaseMaturity + 10);

                // Get set of transaction IDs present in wallet before staking is started.
                this.transactionsBeforeStaking.Clear();
                foreach (TransactionOutputData transactionData in this.GetTransactionsSnapshot(minerA))
                {
                    this.transactionsBeforeStaking.Add(transactionData.Id);
                }

                // Start staking on the node.
                var minter = minerA.FullNode.NodeService<IPosMinting>();
                minter.Stake(new WalletSecret() { WalletName = "mywallet", WalletPassword = "password" });

                // If new transactions are appearing in the wallet, staking has been successful. Due to coin maturity settings the
                // spendable balance of the wallet actually drops after staking, so the wallet balance should not be used to
                // determine whether staking occurred.
                TestBase.WaitLoop(() =>
                {
                    List<TransactionOutputData> transactions = this.GetTransactionsSnapshot(minerA);

                    foreach (TransactionOutputData transactionData in transactions)
                    {
                        if (!this.transactionsBeforeStaking.Contains(transactionData.Id) && (transactionData.IsCoinStake ?? false))
                        {
                            return true;
                        }
                    }

                    return false;
                });

                // build a dictionary of coinstake tx's indexed by tx id.
                foreach (var tx in this.GetTransactionsSnapshot(minerA))
                {
                    this.transactionLookup[tx.Id] = tx;
                }

                TestBase.WaitLoop(() =>
                {
                    List<TransactionOutputData> transactions = this.GetTransactionsSnapshot(minerA);

                    foreach (TransactionOutputData transactionData in transactions)
                    {
                        if (!this.transactionsBeforeStaking.Contains(transactionData.Id) && (transactionData.IsCoinStake ?? false))
                        {
                            Transaction coinstakeTransaction = minerA.FullNode.Network.CreateTransaction(transactionData.Hex);
                            var balance = new Money(0);

                            // Add coinstake outputs to balance.
                            foreach (TxOut output in coinstakeTransaction.Outputs)
                            {
                                balance += output.Value;
                            }

                            // Subtract coinstake inputs from balance.
                            foreach (TxIn input in coinstakeTransaction.Inputs)
                            {
                                this.transactionLookup.TryGetValue(input.PrevOut.Hash, out TransactionOutputData prevTransactionData);

                                if (prevTransactionData == null)
                                    continue;

                                Transaction prevTransaction = minerA.FullNode.Network.CreateTransaction(prevTransactionData.Hex);

                                balance -= prevTransaction.Outputs[input.PrevOut.N].Value;
                            }

                            Assert.Equal(minerA.FullNode.Network.Consensus.ProofOfStakeReward, balance);

                            return true;
                        }
                    }

                    return false;
                });
            }
        }

        /// <summary>
        /// Returns a snapshot of the current transactions by coin type in the first wallet.
        /// </summary>
        /// <returns>A list of TransactionData.</returns>
        private List<TransactionOutputData> GetTransactionsSnapshot(CoreNode node)
        {
            // Enumerate to a list otherwise the enumerable can change during enumeration as new transactions are added to the wallet.
            var wal = node.FullNode.WalletManager().Wallets.First();
            return wal.GetAllTransactions().ToList();
        }
    }
}