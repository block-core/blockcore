using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public BroadcasterManager(IConnectionManager connectionManager, ISignals signals, IEnumerable<IBroadcastCheck> broadcastChecks)
        {
            Guard.NotNull(connectionManager, nameof(connectionManager));

            this.connectionManager = connectionManager;
            this.signals = signals;
            this.broadcastChecks = broadcastChecks;
            this.Broadcasts = new ConcurrentHashSet<BroadcastTransactionStateChanedEntry>();
        }

        public void OnTransactionStateChanged(BroadcastTransactionStateChanedEntry entry)
        {
            this.signals.Publish(new TransactionBroadcastEvent(this, entry));
        }

        /// <summary>Transactions to broadcast.</summary>
        private ConcurrentHashSet<BroadcastTransactionStateChanedEntry> Broadcasts { get; }

        /// <summary>Retrieves a transaction with provided hash from the collection of transactions to broadcast.</summary>
        /// <param name="transactionHash">Hash of the transaction to retrieve.</param>
        public BroadcastTransactionStateChanedEntry GetTransaction(uint256 transactionHash)
        {
            BroadcastTransactionStateChanedEntry txEntry = this.Broadcasts.FirstOrDefault(x => x.Transaction.GetHash() == transactionHash);
            return txEntry ?? null;
        }

        /// <summary>Adds or updates a transaction from the collection of transactions to broadcast.</summary>
        public void AddOrUpdate(Transaction transaction, TransactionBroadcastState transactionBroadcastState, string errorMessage = null)
        {
            BroadcastTransactionStateChanedEntry broadcastEntry = this.Broadcasts.FirstOrDefault(x => x.Transaction.GetHash() == transaction.GetHash());

            if (broadcastEntry == null)
            {
                broadcastEntry = new BroadcastTransactionStateChanedEntry(transaction, transactionBroadcastState, errorMessage);
                this.Broadcasts.Add(broadcastEntry);
                this.OnTransactionStateChanged(broadcastEntry);
            }
            else if (broadcastEntry.TransactionBroadcastState != transactionBroadcastState)
            {
                broadcastEntry.TransactionBroadcastState = transactionBroadcastState;
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
                    this.AddOrUpdate(transaction, TransactionBroadcastState.CantBroadcast, error);
                    return;
                }
            }

            await this.PropagateTransactionToPeersAsync(transaction, this.connectionManager.ConnectedPeers.ToList()).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends transaction to peers.
        /// </summary>
        /// <param name="transaction">Transaction that will be propagated.</param>
        /// <param name="peers">Peers to whom we will propagate the transaction.</param>
        protected async Task PropagateTransactionToPeersAsync(Transaction transaction, List<INetworkPeer> peers)
        {
            this.AddOrUpdate(transaction, TransactionBroadcastState.ToBroadcast);

            var invPayload = new InvPayload(transaction);

            foreach (INetworkPeer peer in peers)
            {
                try
                {
                    await peer.SendMessageAsync(invPayload).ConfigureAwait(false);
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