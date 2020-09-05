﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Features.Wallet.Tests;
using Blockcore.Features.Wallet.Types;
using Blockcore.Tests.Common;
using DBreeze.Utils;
using NBitcoin;
using NBitcoin.Crypto;
using Newtonsoft.Json;

namespace Blockcore.Tests.Wallet.Common
{
    /// <summary>
    /// Helper class containing a bunch of methods used for testing the wallet functionality.
    /// </summary>
    public class WalletTestsHelpers
    {
        public static HdAccount CreateAccount(string name, int index = 0)
        {
            return new HdAccount
            {
                Name = name,
                HdPath = "1/2/3/4/5",
                Index = index
            };
        }

        public static SpendingDetails CreateSpendingDetails(TransactionOutputData changeTransaction, PaymentDetails paymentDetails)
        {
            var spendingDetails = new SpendingDetails
            {
                TransactionId = changeTransaction.Id,
                CreationTime = new DateTimeOffset(new DateTime(2017, 6, 23, 1, 2, 3)),
                BlockHeight = changeTransaction.BlockHeight
            };

            spendingDetails.Payments.Add(paymentDetails);
            return spendingDetails;
        }

        public static PaymentDetails CreatePaymentDetails(Money amount, HdAddress destinationAddress)
        {
            return new PaymentDetails
            {
                Amount = amount,
                DestinationAddress = destinationAddress.Address,
                DestinationScriptPubKey = destinationAddress.ScriptPubKey
            };
        }

        public static TransactionOutputData CreateTransaction(uint256 id, Money amount, int? blockHeight, SpendingDetails spendingDetails = null, DateTimeOffset? creationTime = null, Script script = null, string address = null, int accountIndex = 0)
        {
            if (creationTime == null)
            {
                creationTime = new DateTimeOffset(new DateTime(2017, 6, 23, 1, 2, 3));
            }

            return new TransactionOutputData
            {
                OutPoint = new OutPoint(id, blockHeight ?? 1),
                Address = address ?? script?.ToHex(),
                Amount = amount,
                AccountIndex = accountIndex,
                Id = id,
                CreationTime = creationTime.Value,
                BlockHeight = blockHeight,
                SpendingDetails = spendingDetails,
                ScriptPubKey = script
            };
        }

        public static HdAddress CreateAddress(bool changeAddress = false)
        {
            string hdPath = "1/2/3/4/5";
            if (changeAddress)
            {
                hdPath = "1/2/3/4/1";
            }
            var key = new Key();
            var address = new HdAddress
            {
                Address = key.PubKey.GetAddress(KnownNetworks.Main).ToString(),
                HdPath = hdPath,
                ScriptPubKey = key.ScriptPubKey
            };

            return address;
        }

        public static ChainedHeader AppendBlock(Network network, ChainedHeader previous = null, params ChainIndexer[] chainsIndexer)
        {
            ChainedHeader last = null;
            uint nonce = RandomUtils.GetUInt32();
            foreach (ChainIndexer chain in chainsIndexer)
            {
                Block block = network.CreateBlock();
                block.AddTransaction(network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.HashPrevBlock = previous == null ? chain.Tip.HashBlock : previous.HashBlock;
                block.Header.Nonce = nonce;
                if (!chain.TrySetTip(block.Header, out last))
                    throw new InvalidOperationException("Previous not existing");
            }
            return last;
        }

        public static (ChainedHeader ChainedHeader, Block Block) AppendBlock(Network network, ChainedHeader previous, ChainIndexer chainIndexer)
        {
            ChainedHeader last = null;
            uint nonce = RandomUtils.GetUInt32();
            Block block = network.CreateBlock();

            block.AddTransaction(network.CreateTransaction());
            block.UpdateMerkleRoot();
            block.Header.HashPrevBlock = previous == null ? chainIndexer.Tip.HashBlock : previous.HashBlock;
            block.Header.Nonce = nonce;
            if (!chainIndexer.TrySetTip(block.Header, out last))
                throw new InvalidOperationException("Previous not existing");

            return (last, block);
        }

        public static TransactionBuildContext CreateContext(Network network, WalletAccountReference accountReference, string password,
            Script destinationScript, Money amount, FeeType feeType, int minConfirmations)
        {
            return new TransactionBuildContext(network)
            {
                AccountReference = accountReference,
                MinConfirmations = minConfirmations,
                FeeType = feeType,
                WalletPassword = password,
                Recipients = new[] { new Recipient { Amount = amount, ScriptPubKey = destinationScript } }.ToList()
            };
        }

        public static Features.Wallet.Types.Wallet CreateWallet(string name)
        {
            return new Features.Wallet.Types.Wallet
            {
                Name = name,
                AccountsRoot = new List<AccountRoot>(),
                walletStore = new WalletMemoryStore(),
                BlockLocator = null
            };
        }

        public static Features.Wallet.Types.Wallet GenerateBlankWallet(string name, string password)
        {
            return GenerateBlankWalletWithExtKey(name, password).wallet;
        }

        public static (Features.Wallet.Types.Wallet wallet, ExtKey key) GenerateBlankWalletWithExtKey(string name, string password)
        {
            var mnemonic = new Mnemonic("grass industry beef stereo soap employ million leader frequent salmon crumble banana");
            ExtKey extendedKey = mnemonic.DeriveExtKey(password);

            var walletFile = new Features.Wallet.Types.Wallet
            {
                Name = name,
                EncryptedSeed = extendedKey.PrivateKey.GetEncryptedBitcoinSecret(password, KnownNetworks.Main).ToWif(),
                ChainCode = extendedKey.ChainCode,
                CreationTime = DateTimeOffset.Now,
                Network = KnownNetworks.Main,
                walletStore = new WalletMemoryStore(),
                BlockLocator = new List<uint256>() { KnownNetworks.Main.GenesisHash },
                AccountsRoot = new List<AccountRoot> { new AccountRoot() { Accounts = new List<HdAccount>(), CoinType = KnownNetworks.Main.Consensus.CoinType, LastBlockSyncedHash = KnownNetworks.Main.GenesisHash, LastBlockSyncedHeight = 0 } },
            };

            var data = walletFile.walletStore.GetData();
            data.BlockLocator = walletFile.BlockLocator;
            data.WalletName = walletFile.Name;
            data.WalletTip = new Utilities.HashHeightPair(KnownNetworks.Main.GenesisHash, 0);
            walletFile.walletStore.SetData(data);

            return (walletFile, extendedKey);
        }

        public static Block AppendTransactionInNewBlockToChain(ChainIndexer chainIndexer, Transaction transaction)
        {
            ChainedHeader last = null;
            uint nonce = RandomUtils.GetUInt32();
            Block block = chainIndexer.Network.Consensus.ConsensusFactory.CreateBlock();
            block.AddTransaction(transaction);
            block.UpdateMerkleRoot();
            block.Header.HashPrevBlock = chainIndexer.Tip.HashBlock;
            block.Header.Nonce = nonce;
            if (!chainIndexer.TrySetTip(block.Header, out last))
                throw new InvalidOperationException("Previous not existing");

            return block;
        }

        public static Transaction SetupValidTransaction(Features.Wallet.Types.Wallet wallet, string password, HdAddress spendingAddress, PubKey destinationPubKey, HdAddress changeAddress, Money amount, Money fee)
        {
            return SetupValidTransaction(wallet, password, spendingAddress, destinationPubKey.ScriptPubKey, changeAddress, amount, fee);
        }

        public static Transaction SetupValidTransaction(Features.Wallet.Types.Wallet wallet, string password, HdAddress spendingAddress, Script destinationScript, HdAddress changeAddress, Money amount, Money fee)
        {
            TransactionOutputData spendingTransaction = wallet.walletStore.GetForAddress(spendingAddress.Address).ElementAt(0);
            spendingTransaction.Address = spendingAddress.Address;

            var coin = new Coin(spendingTransaction.Id, (uint)spendingTransaction.Index, spendingTransaction.Amount, spendingTransaction.ScriptPubKey);

            Key privateKey = Key.Parse(wallet.EncryptedSeed, password, wallet.Network);

            var builder = new TransactionBuilder(wallet.Network);
            Transaction tx = builder
                .AddCoins(new List<Coin> { coin })
                .AddKeys(new ExtKey(privateKey, wallet.ChainCode).Derive(new KeyPath(spendingAddress.HdPath)).GetWif(wallet.Network))
                .Send(destinationScript, amount)
                .SetChange(changeAddress.ScriptPubKey)
                .SendFees(fee)
                .BuildTransaction(true);

            if (!builder.Verify(tx))
            {
                throw new WalletException("Could not build transaction, please make sure you entered the correct data.");
            }

            return tx;
        }

        public static void AddAddressesToWallet(WalletManager walletManager, int count)
        {
            foreach (Features.Wallet.Types.Wallet wallet in walletManager.Wallets)
            {
                wallet.AccountsRoot.Add(new AccountRoot()
                {
                    LastBlockSyncedHash = new uint256(0),
                    LastBlockSyncedHeight = 0,
                    CoinType = KnownCoinTypes.Bitcoin,
                    Accounts = new List<HdAccount>
                    {
                        new HdAccount
                        {
                            ExternalAddresses = GenerateAddresses(count),
                            InternalAddresses = GenerateAddresses(count)
                        },
                        new HdAccount
                        {
                            ExternalAddresses = GenerateAddresses(count),
                            InternalAddresses = GenerateAddresses(count)
                        } }
                });
            }
        }

        public static HdAddress CreateAddressWithoutTransaction(int index, string addressName)
        {
            return new HdAddress
            {
                Index = index,
                Address = addressName,
                ScriptPubKey = new Script(),
                //Transactions = new List<TransactionData>()
            };
        }

        public static HdAddress CreateAddressWithEmptyTransaction(int index, string addressName)
        {
            return new HdAddress
            {
                Index = index,
                Address = addressName,
                ScriptPubKey = new Script(),
                //Transactions = new List<TransactionData> { new TransactionData() }
            };
        }

        public static List<HdAddress> GenerateAddresses(int count)
        {
            var addresses = new List<HdAddress>();
            for (int i = 0; i < count; i++)
            {
                var key = new Key().ScriptPubKey;

                var address = new HdAddress
                {
                    Address = key.ToString(),
                    ScriptPubKey = key
                };
                addresses.Add(address);
            }
            return addresses;
        }

        public static (ExtKey ExtKey, string ExtPubKey) GenerateAccountKeys(Features.Wallet.Types.Wallet wallet, string password, string keyPath)
        {
            var accountExtKey = new ExtKey(Key.Parse(wallet.EncryptedSeed, password, wallet.Network), wallet.ChainCode);
            string accountExtendedPubKey = accountExtKey.Derive(new KeyPath(keyPath)).Neuter().ToString(wallet.Network);
            return (accountExtKey, accountExtendedPubKey);
        }

        public static (PubKey PubKey, BitcoinPubKeyAddress Address) GenerateAddressKeys(Features.Wallet.Types.Wallet wallet, string accountExtendedPubKey, string keyPath)
        {
            PubKey addressPubKey = ExtPubKey.Parse(accountExtendedPubKey).Derive(new KeyPath(keyPath)).PubKey;
            BitcoinPubKeyAddress address = addressPubKey.GetAddress(wallet.Network);

            return (addressPubKey, address);
        }

        public static ChainIndexer GenerateChainWithHeight(int blockAmount, Network network)
        {
            var chain = new ChainIndexer(network);
            uint nonce = RandomUtils.GetUInt32();
            uint256 prevBlockHash = chain.Genesis.HashBlock;
            for (int i = 0; i < blockAmount; i++)
            {
                Block block = network.Consensus.ConsensusFactory.CreateBlock();
                block.AddTransaction(network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.BlockTime = new DateTimeOffset(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i));
                block.Header.HashPrevBlock = prevBlockHash;
                block.Header.Nonce = nonce;
                chain.SetTip(block.Header);
                prevBlockHash = block.GetHash();
            }

            return chain;
        }

        /// <summary>
        /// Creates a set of chains 'forking' at a specific block height. You can see the left chain as the old one and the right as the new chain.
        /// </summary>
        /// <param name="blockAmount">Amount of blocks on each chain.</param>
        /// <param name="network">The network to use</param>
        /// <param name="forkBlock">The height at which to put the fork.</param>
        /// <returns></returns>
        public static (ChainIndexer LeftChain, ChainIndexer RightChain, List<Block> LeftForkBlocks, List<Block> RightForkBlocks)
            GenerateForkedChainAndBlocksWithHeight(int blockAmount, Network network, int forkBlock)
        {
            var rightchain = new ChainIndexer(network);
            var leftchain = new ChainIndexer(network);
            uint256 prevBlockHash = rightchain.Genesis.HashBlock;
            var leftForkBlocks = new List<Block>();
            var rightForkBlocks = new List<Block>();

            // build up left fork fully and right fork until forkblock
            uint256 forkBlockPrevHash = null;
            for (int i = 0; i < blockAmount; i++)
            {
                Block block = network.Consensus.ConsensusFactory.CreateBlock();
                block.AddTransaction(network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.HashPrevBlock = prevBlockHash;
                block.Header.Nonce = RandomUtils.GetUInt32();
                leftchain.SetTip(block.Header);

                if (leftchain.Height == forkBlock)
                {
                    forkBlockPrevHash = block.GetHash();
                }
                prevBlockHash = block.GetHash();
                leftForkBlocks.Add(block);

                if (rightchain.Height < forkBlock)
                {
                    rightForkBlocks.Add(block);
                    rightchain.SetTip(block.Header);
                }
            }

            // build up the right fork further.
            for (int i = forkBlock; i < blockAmount; i++)
            {
                Block block = network.Consensus.ConsensusFactory.CreateBlock();
                block.AddTransaction(network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.HashPrevBlock = forkBlockPrevHash;
                block.Header.Nonce = RandomUtils.GetUInt32();
                rightchain.SetTip(block.Header);
                forkBlockPrevHash = block.GetHash();
                rightForkBlocks.Add(block);
            }

            // if all blocks are on both sides the fork fails.
            if (leftForkBlocks.All(l => rightForkBlocks.Select(r => r.GetHash()).Contains(l.GetHash())))
            {
                throw new InvalidOperationException("No fork created.");
            }

            return (leftchain, rightchain, leftForkBlocks, rightForkBlocks);
        }

        public static (ChainIndexer Chain, List<Block> Blocks) GenerateChainAndBlocksWithHeight(int blockAmount, Network network)
        {
            var chain = new ChainIndexer(network);
            uint nonce = RandomUtils.GetUInt32();
            uint256 prevBlockHash = chain.Genesis.HashBlock;
            var blocks = new List<Block>();
            for (int i = 0; i < blockAmount; i++)
            {
                Block block = network.Consensus.ConsensusFactory.CreateBlock();
                block.AddTransaction(network.CreateTransaction());
                block.UpdateMerkleRoot();
                block.Header.HashPrevBlock = prevBlockHash;
                block.Header.Nonce = nonce;
                chain.SetTip(block.Header);
                prevBlockHash = block.GetHash();
                blocks.Add(block);
            }

            return (chain, blocks);
        }

        public static ChainIndexer PrepareChainWithBlock()
        {
            var chain = new ChainIndexer(KnownNetworks.StratisMain);
            uint nonce = RandomUtils.GetUInt32();
            Block block = KnownNetworks.StratisMain.CreateBlock();
            block.AddTransaction(KnownNetworks.StratisMain.CreateTransaction());
            block.UpdateMerkleRoot();
            block.Header.HashPrevBlock = chain.Genesis.HashBlock;
            block.Header.Nonce = nonce;
            block.Header.BlockTime = DateTimeOffset.Now;
            chain.SetTip(block.Header);
            return chain;
        }

        public static ICollection<HdAddress> CreateSpentTransactionsOfBlockHeights(WalletMemoryStore store, Network network, params int[] blockHeights)
        {
            var addresses = new List<HdAddress>();

            foreach (int height in blockHeights)
            {
                var key = new Key();
                var address = new HdAddress
                {
                    Address = key.PubKey.GetAddress(network).ToString(),
                    ScriptPubKey = key.ScriptPubKey,
                };

                store.Add(new List<TransactionOutputData> {
                        new TransactionOutputData
                        {
                            Address = address.Address,
                            OutPoint = new OutPoint(new uint256(Hashes.Hash256(key.PubKey.ToBytes())), height),
                            BlockHeight = height,
                            Amount = new Money(new Random().Next(500000, 1000000)),
                            SpendingDetails = new SpendingDetails(),
                            Id = new uint256(),
                        } });

                addresses.Add(address);
            }

            return addresses;
        }

        public static ICollection<HdAddress> CreateUnspentTransactionsOfBlockHeights(WalletMemoryStore store, Network network, params int[] blockHeights)
        {
            var addresses = new List<HdAddress>();

            foreach (int height in blockHeights)
            {
                var key = new Key();
                var address = new HdAddress
                {
                    Address = key.PubKey.GetAddress(network).ToString(),
                    ScriptPubKey = key.ScriptPubKey,
                    //Transactions = new List<TransactionData> {
                    //    new TransactionData
                    //    {
                    //        BlockHeight = height,
                    //        Amount = new Money(new Random().Next(500000, 1000000))
                    //    }
                    //}
                };

                store.Add(new List<TransactionOutputData>
                {
                        new TransactionOutputData
                        {
                            OutPoint = new OutPoint( new uint256(Hashes.SHA256(key.PubKey.ToBytes())), height),
                            Address = address.Address,
                            BlockHeight = height,
                            Amount = new Money(new Random().Next(500000, 1000000))
                        }
                });

                addresses.Add(address);
            }

            return addresses;
        }

        public static TransactionOutputData CreateTransactionDataFromFirstBlock((ChainIndexer chain, uint256 blockHash, Block block) chainInfo)
        {
            Transaction transaction = chainInfo.block.Transactions[0];

            var addressTransaction = new TransactionOutputData
            {
                OutPoint = new OutPoint(transaction, 0),
                Address = transaction.Outputs[0].ScriptPubKey.ToHex(),
                Amount = transaction.TotalOut,
                BlockHash = chainInfo.blockHash,
                BlockHeight = chainInfo.chain.GetHeader(chainInfo.blockHash).Height,
                CreationTime = DateTimeOffset.FromUnixTimeSeconds(chainInfo.block.Header.Time),
                Id = transaction.GetHash(),
                Index = 0,
                ScriptPubKey = transaction.Outputs[0].ScriptPubKey,
            };

            return addressTransaction;
        }

        public static (ChainIndexer chain, uint256 blockhash, Block block) CreateChainAndCreateFirstBlockWithPaymentToAddress(Network network, HdAddress address)
        {
            var chain = new ChainIndexer(network);

            Block block = network.Consensus.ConsensusFactory.CreateBlock();
            block.Header.HashPrevBlock = chain.Tip.HashBlock;
            block.Header.Bits = block.Header.GetWorkRequired(network, chain.Tip);
            block.Header.UpdateTime(DateTimeOffset.UtcNow, network, chain.Tip);

            Transaction coinbase = network.CreateTransaction();
            coinbase.AddInput(TxIn.CreateCoinbase(chain.Height + 1));
            coinbase.AddOutput(new TxOut(network.GetReward(chain.Height + 1), address.ScriptPubKey));

            block.AddTransaction(coinbase);
            block.Header.Nonce = 0;
            block.UpdateMerkleRoot();
            block.Header.PrecomputeHash();

            chain.SetTip(block.Header);

            return (chain, block.GetHash(), block);
        }

        public static List<Block> AddBlocksWithCoinbaseToChain(WalletMemoryStore store, Network network, ChainIndexer chainIndexer, HdAddress address, int blocks = 1)
        {
            var blockList = new List<Block>();

            for (int i = 0; i < blocks; i++)
            {
                Block block = network.Consensus.ConsensusFactory.CreateBlock();
                block.Header.HashPrevBlock = chainIndexer.Tip.HashBlock;
                block.Header.Bits = block.Header.GetWorkRequired(network, chainIndexer.Tip);
                block.Header.UpdateTime(DateTimeOffset.UtcNow, network, chainIndexer.Tip);

                Transaction coinbase = network.CreateTransaction();
                coinbase.AddInput(TxIn.CreateCoinbase(chainIndexer.Height + 1));
                coinbase.AddOutput(new TxOut(network.GetReward(chainIndexer.Height + 1), address.ScriptPubKey));

                block.AddTransaction(coinbase);
                block.Header.Nonce = 0;
                block.UpdateMerkleRoot();
                block.Header.PrecomputeHash();

                chainIndexer.SetTip(block.Header);

                var addressTransaction = new TransactionOutputData
                {
                    OutPoint = new OutPoint(coinbase.GetHash(), 0),
                    Address = address.Address,
                    Amount = coinbase.TotalOut,
                    BlockHash = block.GetHash(),
                    BlockHeight = chainIndexer.GetHeader(block.GetHash()).Height,
                    CreationTime = DateTimeOffset.FromUnixTimeSeconds(block.Header.Time),
                    Id = coinbase.GetHash(),
                    Index = 0,
                    ScriptPubKey = coinbase.Outputs[0].ScriptPubKey,
                };

                store.InsertOrUpdate(addressTransaction);
                blockList.Add(block);
            }

            return blockList;
        }
    }

    public class WalletFixture : IDisposable
    {
        private readonly Dictionary<(string, string), Features.Wallet.Types.Wallet> walletsGenerated;

        public WalletFixture()
        {
            this.walletsGenerated = new Dictionary<(string, string), Features.Wallet.Types.Wallet>();
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Creates a new wallet.
        /// </summary>
        /// <remarks>
        /// If it's the first time this wallet is created within this class, it is added to a collection for use by other tests.
        /// If the same parameters have already been used to create a wallet, the wallet will be retrieved from the internal collection and a copy of this wallet will be returned.
        /// </remarks>
        /// <param name="name">The name.</param>
        /// <param name="password">The password.</param>
        /// <returns>The generated wallet.</returns>
        public Features.Wallet.Types.Wallet GenerateBlankWallet(string name, string password)
        {
            if (this.walletsGenerated.TryGetValue((name, password), out Features.Wallet.Types.Wallet existingWallet))
            {
                string serializedExistingWallet = JsonConvert.SerializeObject(existingWallet, Formatting.None);
                var wal1 = JsonConvert.DeserializeObject<Features.Wallet.Types.Wallet>(serializedExistingWallet);
                wal1.BlockLocator = existingWallet.BlockLocator;
                wal1.AccountsRoot.Single().LastBlockSyncedHash = existingWallet.AccountsRoot.Single().LastBlockSyncedHash;
                wal1.AccountsRoot.Single().LastBlockSyncedHeight = existingWallet.AccountsRoot.Single().LastBlockSyncedHeight;
                wal1.walletStore = new WalletMemoryStore();
                var data1 = wal1.walletStore.GetData();
                data1.BlockLocator = existingWallet.BlockLocator;
                data1.WalletName = existingWallet.Name;
                data1.WalletTip = new Utilities.HashHeightPair(existingWallet.AccountsRoot.Single().LastBlockSyncedHash, existingWallet.AccountsRoot.Single().LastBlockSyncedHeight.Value);
                wal1.walletStore.SetData(data1);

                return wal1;
            }

            Features.Wallet.Types.Wallet newWallet = WalletTestsHelpers.GenerateBlankWallet(name, password);
            this.walletsGenerated.Add((name, password), newWallet);

            string serializedNewWallet = JsonConvert.SerializeObject(newWallet, Formatting.None);
            var wal = JsonConvert.DeserializeObject<Features.Wallet.Types.Wallet>(serializedNewWallet);
            wal.walletStore = new WalletMemoryStore();
            wal.BlockLocator = newWallet.BlockLocator;
            wal.AccountsRoot.Single().LastBlockSyncedHash = newWallet.AccountsRoot.Single().LastBlockSyncedHash;
            wal.AccountsRoot.Single().LastBlockSyncedHeight = newWallet.AccountsRoot.Single().LastBlockSyncedHeight;

            var data = wal.walletStore.GetData();
            data.BlockLocator = wal.BlockLocator;
            data.WalletName = wal.Name;
            data.WalletTip = new Utilities.HashHeightPair(wal.AccountsRoot.Single().LastBlockSyncedHash, wal.AccountsRoot.Single().LastBlockSyncedHeight.Value);
            wal.walletStore.SetData(data);
            return wal;
        }
    }
}