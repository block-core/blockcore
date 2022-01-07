using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Interfaces;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol.Payloads;
using Blockcore.Signals;
using Blockcore.Utilities;
using ConcurrentCollections;
using NBitcoin;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcasterManager : IBroadcasterManager
    {
        protected readonly IConnectionManager connectionManager;
        private readonly ISignals signals;
        private readonly IEnumerable<IBroadcastCheck> broadcastChecks;

        private Dictionary<uint256, BroadcastTransactionStateChanedEntry> Broadcasts { get; }

        public bool CanRespondToTrxGetData { get; set; }

        public BroadcasterManager(IConnectionManager connectionManager, ISignals signals, IEnumerable<IBroadcastCheck> broadcastChecks)
        {
            Guard.NotNull(connectionManager, nameof(connectionManager));

            this.connectionManager = connectionManager;
            this.signals = signals;
            this.broadcastChecks = broadcastChecks;
            this.Broadcasts = new Dictionary<uint256, BroadcastTransactionStateChanedEntry>();
            this.CanRespondToTrxGetData = true;
        }

        public void OnTransactionStateChanged(BroadcastTransactionStateChanedEntry entry)
        {
            this.signals.Publish(new TransactionBroadcastEvent(this, entry));
        }

        /// <summary>Retrieves a transaction with provided hash from the collection of transactions to broadcast.</summary>
        /// <param name="transactionHash">Hash of the transaction to retrieve.</param>
        public BroadcastTransactionStateChanedEntry GetTransaction(uint256 transactionHash)
        {
            BroadcastTransactionStateChanedEntry txEntry = this.Broadcasts.TryGet(transactionHash);
            return txEntry ?? null;
        }

        /// <summary>Adds or updates a transaction from the collection of transactions to broadcast.</summary>
        public void AddOrUpdate(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage = null)
        {
            bool changed = false;
            uint256 trxHash = transaction.GetHash();
            BroadcastTransactionStateChanedEntry broadcastEntry = this.Broadcasts.TryGet(trxHash);

            if (broadcastEntry == null)
            {
                broadcastEntry = new BroadcastTransactionStateChanedEntry(transaction, transactionBroadcastState, errorMessage);
                this.Broadcasts.Add(trxHash, broadcastEntry);
                changed = true;
            }
            else
            {
                if (broadcastEntry.TransactionBroadcastState != transactionBroadcastState)
                {
                    broadcastEntry.TransactionBroadcastState = transactionBroadcastState;
                    changed = true;
                }

                if (broadcastEntry.ErrorMessage != errorMessage)
                {
                    broadcastEntry.ErrorMessage = errorMessage;
                    changed = true;
                }
            }

            if (changed)
            {
                this.OnTransactionStateChanged(broadcastEntry);
            }
        }

        public async Task BroadcastTransactionAsync(Transaction transaction)
        {
            Guard.NotNull(transaction, nameof(transaction));

            if (this.IsPropagated(transaction))
            {
                return;
            }

            foreach (IBroadcastCheck broadcastCheck in this.broadcastChecks)
            {
                string error = await broadcastCheck.CheckTransaction(transaction).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(error))
                {
                    this.AddOrUpdate(transaction, TransactionBroadcastState.FailedBroadcast, error);
                    return;
                }
            }

            await this.PropagateTransactionToPeersAsync(transaction).ConfigureAwait(false);
        }

        public async Task<bool> BroadcastTransactionAsync(uint256 trxHash)
        {
            if (this.Broadcasts.TryGetValue(trxHash, out BroadcastTransactionStateChanedEntry entry))
            {
                if (entry.TransactionBroadcastState == TransactionBroadcastState.ReadyToBroadcast ||
                    entry.TransactionBroadcastState == TransactionBroadcastState.Broadcasted)
                {
                    // broadacste
                    await this.PropagateTransactionToPeersAsync(entry.Transaction).ConfigureAwait(false);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sends transaction to peers.
        /// </summary>
        /// <param name="transaction">Transaction that will be propagated.</param>
        protected async Task PropagateTransactionToPeersAsync(Transaction transaction)
        {
            this.AddOrUpdate(transaction, TransactionBroadcastState.ReadyToBroadcast);

            var invPayload = new InvPayload(transaction);

            List<INetworkPeer> peers = this.connectionManager.ConnectedPeers.ToList();

            foreach (INetworkPeer peer in peers)
            {
                try
                {
                    if (peer.IsConnected)
                    {
                        await peer.SendMessageAsync(invPayload).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        /// <summary>Checks if transaction was propagated to any peers on the network.</summary>
        protected bool IsPropagated(Transaction transaction)
        {
            BroadcastTransactionStateChanedEntry broadcastEntry = this.GetTransaction(transaction.GetHash());
            return (broadcastEntry != null) && (broadcastEntry.TransactionBroadcastState == TransactionBroadcastState.Propagated);
        }
    }
}