using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol;
using Blockcore.P2P.Protocol.Behaviors;
using Blockcore.P2P.Protocol.Payloads;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Blockcore.Connection.Broadcasting
{
    public class BroadcasterBehavior : NetworkPeerBehavior
    {
        private readonly Network network;
        private readonly IBroadcasterManager broadcasterManager;

        /// <summary>Instance logger for the memory pool component.</summary>
        private readonly ILogger logger;

        public BroadcasterBehavior(
            Network network,
            IBroadcasterManager broadcasterManager,
            ILogger logger)
        {
            this.logger = logger;
            this.network = network;
            this.broadcasterManager = broadcasterManager;
        }

        public BroadcasterBehavior(
            Network network,
            IBroadcasterManager broadcasterManager,
            ILoggerFactory loggerFactory)
            : this(network, broadcasterManager, loggerFactory.CreateLogger(typeof(BroadcasterBehavior).FullName))
        {
        }

        /// <inheritdoc />
        public override object Clone()
        {
            return new BroadcasterBehavior(this.network, this.broadcasterManager, this.logger);
        }

        /// <summary>
        /// Handler for processing incoming message from the peer.
        /// </summary>
        /// <param name="peer">Peer sending the message.</param>
        /// <param name="message">Incoming message.</param>
        /// <remarks>
        /// TODO: Fix the exception handling of the async event.
        /// </remarks>
        protected async Task OnMessageReceivedAsync(INetworkPeer peer, IncomingMessage message)
        {
            try
            {
                await this.ProcessMessageAsync(peer, message).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.ToString());

                // while in dev catch any unhandled exceptions
                Debugger.Break();
                throw;
            }
        }

        /// <summary>
        /// Handler for processing peer messages.
        /// Handles the following message payloads: TxPayload, MempoolPayload, GetDataPayload, InvPayload.
        /// </summary>
        /// <param name="peer">Peer sending the message.</param>
        /// <param name="message">Incoming message.</param>
        protected async Task ProcessMessageAsync(INetworkPeer peer, IncomingMessage message)
        {
            switch (message.Message.Payload)
            {
                case GetDataPayload getDataPayload:
                    await this.ProcessGetDataPayloadAsync(peer, getDataPayload).ConfigureAwait(false);
                    break;

                case InvPayload invPayload:
                    this.ProcessInvPayload(invPayload);
                    break;
            }
        }

        private void ProcessInvPayload(InvPayload invPayload)
        {
            // if node has transaction we broadcast
            foreach (InventoryVector inv in invPayload.Inventory.Where(x => x.Type == InventoryType.MSG_TX))
            {
                BroadcastTransactionStateChanedEntry txEntry = this.broadcasterManager.GetTransaction(inv.Hash);
                if (txEntry != null)
                {
                    this.broadcasterManager.AddOrUpdate(txEntry.Transaction, TransactionBroadcastState.Propagated);
                }
            }
        }

        protected async Task ProcessGetDataPayloadAsync(INetworkPeer peer, GetDataPayload getDataPayload)
        {
            // If node asks for transaction we want to broadcast.
            foreach (InventoryVector inv in getDataPayload.Inventory.Where(x => x.Type == InventoryType.MSG_TX))
            {
                BroadcastTransactionStateChanedEntry txEntry = this.broadcasterManager.GetTransaction(inv.Hash);
                if ((txEntry != null) && (txEntry.TransactionBroadcastState != TransactionBroadcastState.FailedBroadcast))
                {
                    if (txEntry.CanRespondToGetData && peer.IsConnected)
                    {
                        this.logger.LogDebug("Sending transaction '{0}' to peer '{1}'.", inv.Hash, peer.RemoteSocketEndpoint);
                        await peer.SendMessageAsync(new TxPayload(txEntry.Transaction.WithOptions(peer.SupportedTransactionOptions, this.network.Consensus.ConsensusFactory))).ConfigureAwait(false);
                    }

                    if (txEntry.TransactionBroadcastState == TransactionBroadcastState.ReadyToBroadcast)
                    {
                        this.broadcasterManager.AddOrUpdate(txEntry.Transaction, TransactionBroadcastState.Broadcasted);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void AttachCore()
        {
            this.AttachedPeer.MessageReceived.Register(this.OnMessageReceivedAsync);
        }

        /// <inheritdoc />
        protected override void DetachCore()
        {
            this.AttachedPeer.MessageReceived.Unregister(this.OnMessageReceivedAsync);
        }
    }
}