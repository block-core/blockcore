using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blockcore.Builder;
using Blockcore.Consensus;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.Miner.Interfaces;
using Blockcore.Features.Miner.Staking;
using Blockcore.Features.RPC;
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
using NBitcoin.Protocol;
using Xunit;

namespace Blockcore.IntegrationTests
{
    public class ProofOfStakeSteps
    {
        public readonly NodeBuilder nodeBuilder;
        public CoreNode PremineNodeWithCoins;

        public readonly string PremineNode = "PremineNode";
        public readonly string PremineWallet = "mywallet";
        public readonly string PremineWalletAccount = "account 0";
        public readonly string PremineWalletPassword = "password";

        private readonly HashSet<uint256> transactionsBeforeStaking = new HashSet<uint256>();

        private readonly ConcurrentDictionary<uint256, TransactionOutputData> txLookup = new ConcurrentDictionary<uint256, TransactionOutputData>();

        public ProofOfStakeSteps(string displayName)
        {
            this.nodeBuilder = NodeBuilder.Create(Path.Combine(this.GetType().Name, displayName));
        }

        public void PremineNodeWithWallet(string testId)
        {
            this.PremineNodeWithCoins = this.nodeBuilder.CreateStratisPosNode(new StratisRegTest(), testId).WithWallet().Start();
        }

        public void PremineNodeWithWalletWithOverrides()
        {
            var configParameters = new NodeConfigParameters { { "savetrxhex", "true" } };

            var callback = new Action<IFullNodeBuilder>(builder => builder
                .UseBlockStore()
                .UsePosConsensus()
                .UseMempool()
                .UseWallet()
                .AddPowPosMining()
                .AddRPC()
                .MockIBD()
                .UseTestChainedHeaderTree()
                .OverrideDateTimeProviderFor<MiningFeature>());

            this.PremineNodeWithCoins = this.nodeBuilder.CreateCustomNode(callback, new StratisRegTest(), this.PremineNodeWithCoins.FullNode.Network.Consensus.ConsensusFactory.Protocol.ProtocolVersion, agent: "mint-pmnode", configParameters: configParameters);
            this.PremineNodeWithCoins.WithWallet().Start();
        }

        public void MineGenesisAndPremineBlocks()
        {
            int premineBlockCount = 2;

            var addressUsed = TestHelper.MineBlocks(this.PremineNodeWithCoins, premineBlockCount).AddressUsed;

            var wallet = this.PremineNodeWithCoins.FullNode.WalletManager().Wallets.Single(w => w.Name == "mywallet");
            var allTrx = wallet.walletStore.GetForAddress(addressUsed.Address);

            // Since the pre-mine will not be immediately spendable, the transactions have to be counted directly from the address.
            allTrx.Count().Should().Be(premineBlockCount);

            IConsensus consensus = this.PremineNodeWithCoins.FullNode.Network.Consensus;

            allTrx.Sum(s => s.Amount).Should().Be(consensus.PremineReward + consensus.ProofOfWorkReward);

            var balance = this.PremineNodeWithCoins.FullNode.WalletManager().GetAddressBalance(addressUsed.Address);
            balance.AmountConfirmed.Should().Be(consensus.PremineReward + consensus.ProofOfWorkReward);
        }

        public void MineCoinsToMaturity()
        {
            TestHelper.MineBlocks(this.PremineNodeWithCoins, (int)this.PremineNodeWithCoins.FullNode.Network.Consensus.CoinbaseMaturity);
        }

        public void PremineNodeMinesTenBlocksMoreEnsuringTheyCanBeStaked()
        {
            TestHelper.MineBlocks(this.PremineNodeWithCoins, 10);
        }

        public void PremineNodeStartsStaking()
        {
            // Get set of transaction IDs present in wallet before staking is started.
            this.transactionsBeforeStaking.Clear();
            foreach (TransactionOutputData transactionData in this.GetTransactionsSnapshot())
            {
                this.transactionsBeforeStaking.Add(transactionData.Id);
            }

            var minter = this.PremineNodeWithCoins.FullNode.NodeService<IPosMinting>();
            minter.Stake(new WalletSecret() { WalletName = this.PremineWallet, WalletPassword = this.PremineWalletPassword });
        }

        public void PremineNodeWalletHasEarnedCoinsThroughStaking()
        {
            // If new transactions are appearing in the wallet, staking has been successful. Due to coin maturity settings the
            // spendable balance of the wallet actually drops after staking, so the wallet balance should not be used to
            // determine whether staking occurred.
            TestBase.WaitLoop(() =>
            {
                List<TransactionOutputData> transactions = this.GetTransactionsSnapshot();

                foreach (TransactionOutputData transactionData in transactions)
                {
                    if (!this.transactionsBeforeStaking.Contains(transactionData.Id) && (transactionData.IsCoinStake ?? false))
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        public void PosRewardForAllCoinstakeTransactionsIsCorrect()
        {
            // build a dictionary of coinstake tx's indexed by tx id.
            foreach (var tx in this.GetTransactionsSnapshot())
            {
                this.txLookup[tx.Id] = tx;
            }

            TestBase.WaitLoop(() =>
            {
                List<TransactionOutputData> transactions = this.GetTransactionsSnapshot();

                foreach (TransactionOutputData transactionData in transactions)
                {
                    if (!this.transactionsBeforeStaking.Contains(transactionData.Id) && (transactionData.IsCoinStake ?? false))
                    {
                        Transaction coinstakeTransaction = this.PremineNodeWithCoins.FullNode.Network.CreateTransaction(transactionData.Hex);
                        var balance = new Money(0);

                        // Add coinstake outputs to balance.
                        foreach (TxOut output in coinstakeTransaction.Outputs)
                        {
                            balance += output.Value;
                        }

                        // Subtract coinstake inputs from balance.
                        foreach (TxIn input in coinstakeTransaction.Inputs)
                        {
                            this.txLookup.TryGetValue(input.PrevOut.Hash, out TransactionOutputData prevTransactionData);

                            if (prevTransactionData == null)
                                continue;

                            Transaction prevTransaction = this.PremineNodeWithCoins.FullNode.Network.CreateTransaction(prevTransactionData.Hex);

                            balance -= prevTransaction.Outputs[input.PrevOut.N].Value;
                        }

                        Assert.Equal(this.PremineNodeWithCoins.FullNode.Network.Consensus.ProofOfStakeReward, balance);

                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Returns a snapshot of the current transactions by coin type in the first wallet.
        /// </summary>
        /// <returns>A list of TransactionData.</returns>
        private List<TransactionOutputData> GetTransactionsSnapshot()
        {
            // Enumerate to a list otherwise the enumerable can change during enumeration as new transactions are added to the wallet.
            var wal = this.PremineNodeWithCoins.FullNode.WalletManager().Wallets.First();
            return wal.GetAllTransactions().ToList();
        }
    }
}