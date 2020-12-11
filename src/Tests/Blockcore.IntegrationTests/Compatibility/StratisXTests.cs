﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Blockcore.Builder;
using Blockcore.Features.BlockStore;
using Blockcore.Features.Consensus;
using Blockcore.Features.MemoryPool;
using Blockcore.Features.Miner;
using Blockcore.Features.RPC;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Api.Controllers;
using Blockcore.Features.Wallet.Api.Models;
using Blockcore.Features.Wallet.Types;
using Blockcore.IntegrationTests.Common;
using Blockcore.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Blockcore.IntegrationTests.Common.Extensions;
using Blockcore.IntegrationTests.Common.ReadyData;
using Blockcore.IntegrationTests.Common.TestNetworks;
using Blockcore.Networks;
using Blockcore.Networks.Stratis;
using Blockcore.Tests.Common;
using NBitcoin;
using NBitcoin.Protocol;
using Xunit;

namespace Blockcore.IntegrationTests.Compatibility
{
    public class StratisXTests
    {
        /// <summary>
        /// Tests whether a quantity of blocks mined on SBFN are correctly synced to a stratisX node.
        /// </summary>
        [Fact]
        public void SBFNMinesBlocks_XSyncs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var network = new StratisOverrideRegTest();

                CoreNode stratisXNode = builder.CreateStratisXNode(version: "2.0.0.5").Start();
                CoreNode sbfnNode = builder.CreateStratisPosNode(network).WithWallet().Start();

                RPCClient stratisXRpc = stratisXNode.CreateRPCClient();
                RPCClient sbfnNodeRpc = sbfnNode.CreateRPCClient();

                // TODO: Need to troubleshoot why TestHelper.Connect() does not work here, possibly unsupported RPC method (it seems that addnode does not work for X).
                sbfnNodeRpc.AddNode(stratisXNode.Endpoint, false);

                // TODO: Similarly, the 'generate' RPC call is problematic on X. Possibly returning an unexpected JSON format.
                TestHelper.MineBlocks(sbfnNode, 10);

                var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

                TestBase.WaitLoop(() => sbfnNodeRpc.GetBlockCount() >= 10, cancellationToken: cancellationToken);
                TestBase.WaitLoop(() => sbfnNodeRpc.GetBestBlockHash() == stratisXRpc.GetBestBlockHash(), cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Tests whether a quantity of blocks mined on X are
        /// correctly synced to an SBFN node.
        /// </summary>
        [Fact]
        public void XMinesBlocks_SBFNSyncs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this).WithLogsEnabled())
            {
                var network = new StratisOverrideRegTest();

                CoreNode stratisXNode = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                var callback = new Action<IFullNodeBuilder>(build => build
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .AddRPC());

                var config = new NodeConfigParameters();
                config.Add("whitelist", stratisXNode.Endpoint.ToString());
                config.Add("gateway", "1");

                CoreNode stratisNode = builder
                    .CreateCustomNode(callback, network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, minProtocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION, configParameters: config)
                    .WithWallet().Start();

                RPCClient stratisXRpc = stratisXNode.CreateRPCClient();
                RPCClient stratisNodeRpc = stratisNode.CreateRPCClient();

                stratisNodeRpc.AddNode(stratisXNode.Endpoint, false);

                stratisXRpc.SendCommand(RPCOperations.generate, 10);

                var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

                TestBase.WaitLoop(() => stratisXRpc.GetBlockCount() >= 10, cancellationToken: cancellationToken);
                TestBase.WaitLoop(() => stratisXRpc.GetBestBlockHash() == stratisNodeRpc.GetBestBlockHash(), cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Tests whether a transaction relayed by SBFN appears in the
        /// stratisX mempool. The transaction is then mined into a
        /// block by SBFN, and the block must be accepted by stratisX.
        /// </summary>
        /// <remarks>Takes a while to run as block generation cannot
        /// be sped up for this test.</remarks>
        [Fact(Skip = "Awaiting fix for issue #2468")]
        public void SBFNMinesTransaction_XSyncs()
        {
            // TODO: Currently fails due to issue #2468 (coinbase
            // reward on stratisX cannot be >4. No fees are allowed)

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var network = new StratisOverrideRegTest();

                CoreNode stratisXNode = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                // We do not want the datetime provider to be substituted,
                // so a custom builder callback has to be used.
                var callback = new Action<IFullNodeBuilder>(build => build
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .AddRPC()
                    .UseTestChainedHeaderTree()
                    .MockIBD());

                CoreNode stratisNode = builder.CreateCustomNode(callback, network, protocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION).WithWallet().Start();

                RPCClient stratisXRpc = stratisXNode.CreateRPCClient();
                RPCClient stratisNodeRpc = stratisNode.CreateRPCClient();

                stratisXRpc.AddNode(stratisNode.Endpoint, false);
                stratisNodeRpc.AddNode(stratisXNode.Endpoint, false);

                TestHelper.MineBlocks(stratisNode, 11);

                // It takes a reasonable amount of time for blocks to be generated without
                // the datetime provider substitution.
                var longCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(15)).Token;
                var shortCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

                TestBase.WaitLoop(() => stratisNodeRpc.GetBestBlockHash() == stratisXRpc.GetBestBlockHash(), cancellationToken: longCancellationToken);

                // Send transaction to arbitrary address from SBFN side.
                var alice = new Key().GetBitcoinSecret(network);
                var aliceAddress = alice.GetAddress();
                stratisNodeRpc.WalletPassphrase("password", 60);
                stratisNodeRpc.SendToAddress(aliceAddress, Money.Coins(1.0m));

                TestBase.WaitLoop(() => stratisNodeRpc.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);

                // Transaction should percolate through to X's mempool.
                TestBase.WaitLoop(() => stratisXRpc.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);

                // Now SBFN must mine the block.
                TestHelper.MineBlocks(stratisNode, 1);

                // We expect that X will sync correctly.
                TestBase.WaitLoop(() => stratisNodeRpc.GetBestBlockHash() == stratisXRpc.GetBestBlockHash(), cancellationToken: shortCancellationToken);

                // Sanity check - mempools should both become empty.
                TestBase.WaitLoop(() => stratisNodeRpc.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => stratisXRpc.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
            }
        }

        /// <summary>
        /// This test is necessary because X regards nulldata transactions as non-standard if they have a value of zero assigned.
        /// </summary>
        [Fact]
        public void SBFNCreatesOpReturnTransaction_XSyncs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var network = new StratisOverrideRegTest();

                CoreNode stratisXNode = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                // We do not want the datetime provider to be substituted,
                // so a custom builder callback has to be used.
                var callback = new Action<IFullNodeBuilder>(build => build
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .AddRPC()
                    .UseTestChainedHeaderTree()
                    .MockIBD());

                CoreNode stratisNode = builder.CreateCustomNode(callback, network, protocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION, minProtocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION).WithWallet().Start();

                RPCClient stratisXRpc = stratisXNode.CreateRPCClient();
                RPCClient stratisNodeRpc = stratisNode.CreateRPCClient();

                stratisXRpc.AddNode(stratisNode.Endpoint, false);
                stratisNodeRpc.AddNode(stratisXNode.Endpoint, false);

                TestHelper.MineBlocks(stratisNode, 11);

                // It takes a reasonable amount of time for blocks to be generated without
                // the datetime provider substitution.
                var longCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(15)).Token;
                var shortCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

                TestBase.WaitLoop(() => stratisNodeRpc.GetBestBlockHash() == stratisXRpc.GetBestBlockHash(), cancellationToken: longCancellationToken);

                // Send transaction to arbitrary address from SBFN side.
                var alice = new Key().GetBitcoinSecret(network);
                var aliceAddress = alice.GetAddress();
                //stratisNodeRpc.WalletPassphrase("password", 60);

                var transactionBuildContext = new TransactionBuildContext(stratisNode.FullNode.Network)
                {
                    AccountReference = new WalletAccountReference("mywallet", "account 0"),
                    MinConfirmations = 1,
                    OpReturnData = "test",
                    OpReturnAmount = Money.Coins(0.01m),
                    WalletPassword = "password",
                    Recipients = new List<Recipient>() { new Recipient() { Amount = Money.Coins(1), ScriptPubKey = aliceAddress.ScriptPubKey } }
                };

                var transaction = stratisNode.FullNode.WalletTransactionHandler().BuildTransaction(transactionBuildContext);

                stratisNode.FullNode.NodeController<WalletController>().SendTransaction(new SendTransactionRequest(transaction.ToHex()));

                TestBase.WaitLoop(() => stratisNodeRpc.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);

                // Transaction should percolate through to X's mempool.
                TestBase.WaitLoop(() => stratisXRpc.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);
            }
        }

        [Fact]
        public void XMinesTransaction_SBFNSyncs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var network = new StratisOverrideRegTest();

                CoreNode stratisXNode = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                var callback = new Action<IFullNodeBuilder>(build => build
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .AddRPC());

                var config = new NodeConfigParameters();
                config.Add("whitelist", stratisXNode.Endpoint.ToString());
                config.Add("gateway", "1");

                CoreNode stratisNode = builder
                    .CreateCustomNode(callback, network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, minProtocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION, configParameters: config)
                    .WithWallet().Start();

                RPCClient stratisXRpc = stratisXNode.CreateRPCClient();
                RPCClient stratisNodeRpc = stratisNode.CreateRPCClient();

                stratisXRpc.AddNode(stratisNode.Endpoint, false);
                stratisNodeRpc.AddNode(stratisXNode.Endpoint, false);

                stratisXRpc.SendCommand(RPCOperations.generate, 11);

                var shortCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;

                // Without this there seems to be a race condition between the blocks all getting generated and SBFN syncing high enough to fall through the getbestblockhash check.
                TestBase.WaitLoop(() => stratisXRpc.GetBlockCount() >= 11, cancellationToken: shortCancellationToken);

                TestBase.WaitLoop(() => stratisNodeRpc.GetBestBlockHash() == stratisXRpc.GetBestBlockHash(), cancellationToken: shortCancellationToken);

                // Send transaction to arbitrary address from X side.
                var alice = new Key().GetBitcoinSecret(network);
                var aliceAddress = alice.GetAddress();
                stratisXRpc.SendCommand(RPCOperations.sendtoaddress, aliceAddress.ToString(), 1);

                TestBase.WaitLoop(() => stratisXRpc.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);

                // Transaction should percolate through to SBFN's mempool.
                TestBase.WaitLoop(() => stratisNodeRpc.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);

                // Now X must mine the block.
                stratisXRpc.SendCommand(RPCOperations.generate, 1);
                TestBase.WaitLoop(() => stratisXRpc.GetBlockCount() >= 12, cancellationToken: shortCancellationToken);

                // We expect that SBFN will sync correctly.
                TestBase.WaitLoop(() => stratisNodeRpc.GetBestBlockHash() == stratisXRpc.GetBestBlockHash(), cancellationToken: shortCancellationToken);

                // Sanity check - mempools should both become empty.
                TestBase.WaitLoop(() => stratisNodeRpc.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => stratisXRpc.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
            }
        }

        /// <summary>
        /// Construct a small network of 3 nodes, X1 - S2 -X3.
        /// X1 generates sufficient blocks to get spendable coins.
        /// X1 creates a transaction sending a coin to an arbitrary address.
        /// S2 and X3 should get the transaction in their mempools.
        /// </summary>
        [Fact]
        [Trait("Unstable", "True")]
        public void Transaction_CreatedByXNode_TraversesSBFN_ReachesSecondXNode()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var network = new StratisOverrideRegTest();

                CoreNode xNode1 = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                var callback = new Action<IFullNodeBuilder>(build => build
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .AddRPC());

                var config = new NodeConfigParameters();
                config.Add("whitelist", xNode1.Endpoint.ToString());
                config.Add("gateway", "1");

                CoreNode sbfnNode2 = builder
                    .CreateCustomNode(callback, network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, minProtocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION, configParameters: config)
                    .WithWallet().Start();

                CoreNode xNode3 = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                RPCClient xRpc1 = xNode1.CreateRPCClient();
                RPCClient sbfnRpc2 = sbfnNode2.CreateRPCClient();
                RPCClient xRpc3 = xNode3.CreateRPCClient();

                // Connect the nodes linearly. X does not appear to respond properly to the addnode RPC so SBFN needs to initiate.
                sbfnRpc2.AddNode(xNode1.Endpoint, false);
                sbfnRpc2.AddNode(xNode3.Endpoint, false);

                xRpc1.SendCommand(RPCOperations.generate, 11);

                var shortCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

                TestBase.WaitLoop(() => xRpc1.GetBlockCount() >= 11, cancellationToken: shortCancellationToken);

                TestBase.WaitLoop(() => xRpc1.GetBestBlockHash() == sbfnRpc2.GetBestBlockHash(), cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => xRpc1.GetBestBlockHash() == xRpc3.GetBestBlockHash(), cancellationToken: shortCancellationToken);

                // Send transaction to arbitrary address.
                var alice = new Key().GetBitcoinSecret(network);
                var aliceAddress = alice.GetAddress();
                xRpc1.SendCommand(RPCOperations.sendtoaddress, aliceAddress.ToString(), 1);

                TestBase.WaitLoop(() => xRpc1.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => sbfnRpc2.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => xRpc3.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);
            }
        }

        [Fact]
        [Trait("Unstable", "True")]
        public void GatewayNodeCanSyncBeforeAndAfterLastCheckpointPowAndPoS()
        {
            Network network = new StratisMain10KCheckpoint();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this).WithLogsEnabled())
            {
                CoreNode stratisXNode = builder.CreateMainnetStratisXNode()
                    .WithReadyBlockchainData(ReadyBlockchain.StratisXMainnet15K);

                var gatewayParameters = new NodeConfigParameters();
                gatewayParameters.Add("regtest", "0");
                gatewayParameters.Add("gateway", "1");
                gatewayParameters.Add("txindex", "0");
                gatewayParameters.Add("whitelist", stratisXNode.Endpoint.ToString());
                CoreNode gatewayNode =
                    builder.CreateStratisPosNode(network, configParameters: gatewayParameters, isGateway: true)
                    .WithReadyBlockchainData(ReadyBlockchain.StratisMainnet9500);

                gatewayNode.Start();
                stratisXNode.Start();

                RPCClient stratisXRpc = stratisXNode.CreateRPCClient();
                RPCClient gatewayNodeRpc = gatewayNode.CreateRPCClient();

                gatewayNodeRpc.AddNode(stratisXNode.Endpoint);

                TestBase.WaitLoop(() => gatewayNode.FullNode.ChainIndexer.Height >= 13_000, waitTimeSeconds: 600);
            }
        }

        /// <summary>
        /// Construct a small network of 3 nodes, X1 - S2 -X3.
        /// X1 generates sufficient blocks to get spendable coins.
        /// X1 creates a transaction sending a coin to an arbitrary address.
        /// S2 and X3 should get the transaction in their mempools.
        /// Now X1 mines a block.
        /// S2 and X3 should sync to the new block height.
        /// All mempools should be empty at the end.
        /// </summary>
        [Fact]
        public void Transaction_TraversesNodes_AndIsMined_AndNodesSync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: Add the necessary executables for Linux & OSX
                return;
            }

            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var network = new StratisOverrideRegTest();

                CoreNode xNode1 = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                var callback = new Action<IFullNodeBuilder>(build => build
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseWallet()
                    .AddPowPosMining()
                    .AddRPC());

                var config = new NodeConfigParameters();
                config.Add("whitelist", xNode1.Endpoint.ToString());
                config.Add("gateway", "1");

                CoreNode sbfnNode2 = builder
                    .CreateCustomNode(callback, network, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, minProtocolVersion: ProtocolVersion.POS_PROTOCOL_VERSION, configParameters: config)
                    .WithWallet().Start();

                CoreNode xNode3 = builder.CreateStratisXNode(version: "2.0.0.5").Start();

                RPCClient xRpc1 = xNode1.CreateRPCClient();
                RPCClient sbfnRpc2 = sbfnNode2.CreateRPCClient();
                RPCClient xRpc3 = xNode3.CreateRPCClient();

                sbfnRpc2.AddNode(xNode1.Endpoint, false);
                sbfnRpc2.AddNode(xNode3.Endpoint, false);

                xRpc1.SendCommand(RPCOperations.generate, 11);

                var shortCancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;

                TestBase.WaitLoop(() => xRpc1.GetBlockCount() >= 11, cancellationToken: shortCancellationToken);

                TestBase.WaitLoop(() => xRpc1.GetBestBlockHash() == sbfnRpc2.GetBestBlockHash(), cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => xRpc1.GetBestBlockHash() == xRpc3.GetBestBlockHash(), cancellationToken: shortCancellationToken);

                // Send transaction to arbitrary address.
                var alice = new Key().GetBitcoinSecret(network);
                var aliceAddress = alice.GetAddress();
                xRpc1.SendCommand(RPCOperations.sendtoaddress, aliceAddress.ToString(), 1);

                TestBase.WaitLoop(() => xRpc1.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => sbfnRpc2.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => xRpc3.GetRawMempool().Length == 1, cancellationToken: shortCancellationToken);

                // TODO: Until #2468 is fixed we need an X node to mine the block so it doesn't get rejected.
                xRpc1.SendCommand(RPCOperations.generate, 1);
                TestBase.WaitLoop(() => xRpc1.GetBlockCount() >= 12, cancellationToken: shortCancellationToken);

                // We expect that SBFN and the other X node will sync correctly.
                TestBase.WaitLoop(() => sbfnRpc2.GetBestBlockHash() == xRpc1.GetBestBlockHash(), cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => xRpc3.GetBestBlockHash() == xRpc1.GetBestBlockHash(), cancellationToken: shortCancellationToken);

                // Sanity check - mempools should all become empty.
                TestBase.WaitLoop(() => xRpc1.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => sbfnRpc2.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
                TestBase.WaitLoop(() => xRpc3.GetRawMempool().Length == 0, cancellationToken: shortCancellationToken);
            }
        }
    }
}