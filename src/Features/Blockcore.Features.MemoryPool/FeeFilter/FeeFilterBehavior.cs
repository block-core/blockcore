using System;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.P2P.Peer;
using Blockcore.P2P.Protocol;
using Blockcore.P2P.Protocol.Behaviors;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.Features.MemoryPool.FeeFilter
{
    /// <summary>
    /// Implementing FeeFilter as described in BIP0133
    /// https://github.com/bitcoin/bips/blob/master/bip-0133.mediawiki
    /// </summary>
    public class FeeFilterBehavior : NetworkPeerBehavior
    {
        private readonly ILogger logger;
        private readonly Network network;
        private readonly MempoolSettings settings;
        private readonly MempoolManager mempoolManager;
        private readonly IInitialBlockDownloadState initialBlockDownloadState;
        private readonly ILoggerFactory loggerFactory;
        private readonly INodeLifetime nodeLifetime;
        private readonly IAsyncProvider asyncProvider;
        private MempoolBehavior mempoolBehavior;
        private IAsyncLoop asyncLoop;

        private Money lastSendFilter;

        public FeeFilterBehavior(
            Network network,
            MempoolSettings settings,
            MempoolManager mempoolManager,
            IInitialBlockDownloadState initialBlockDownloadState,
            ILoggerFactory loggerFactory,
            INodeLifetime nodeLifetime,
            IAsyncProvider asyncProvider)
        {
            this.network = network;
            this.settings = settings;
            this.mempoolManager = mempoolManager;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.loggerFactory = loggerFactory;
            this.nodeLifetime = nodeLifetime;
            this.asyncProvider = asyncProvider;

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        private Task ProcessFeeFilterAsync(INetworkPeer peer, FeeFilterPayload feeFilter)
        {
            if (peer.PeerVersion.Relay)
            {
                if (feeFilter.NewFeeFilter > 0)
                {
                    if (this.mempoolBehavior == null)
                        this.mempoolBehavior = peer.Behavior<MempoolBehavior>();

                    this.mempoolBehavior.MinFeeFilter = feeFilter.NewFeeFilter;

                    this.logger.LogDebug("Received feefilter of `{0}` from peer id: {1}", feeFilter.NewFeeFilter, peer.Connection.Id);
                }
            }

            return Task.CompletedTask;
        }

        private void StartFeeFilterBroadcast(INetworkPeer sender)
        {
            if (this.settings.FeeFilter)
            {
                INetworkPeer peer = sender;

                if (peer.PeerVersion != null
                    && peer.PeerVersion.Relay
                    && peer.PeerVersion.Version >= ProtocolVersion.FEEFILTER_VERSION)
                {
                    if (this.asyncLoop != null)
                    {
                        this.asyncLoop = this.asyncProvider.CreateAndRunAsyncLoop($"MemoryPool.FeeFilter:{peer.Connection.Id}", async token =>
                        {
                            if (this.initialBlockDownloadState.IsInitialBlockDownload())
                            {
                                return;
                            }

                            var feeRate = await this.mempoolManager.GetMempoolMinFeeAsync(MempoolValidator.DefaultMaxMempoolSize * 1000000).ConfigureAwait(false);
                            var currentFilter = feeRate.FeePerK;

                            // We always have a fee filter of at least minRelayTxFee
                            Money filterToSend = Math.Max(currentFilter, new FeeRate(this.network.MinRelayTxFee).FeePerK);

                            if (filterToSend != this.lastSendFilter)
                            {
                                INetworkPeer peer = this.AttachedPeer;

                                if (peer != null && peer.IsConnected)
                                {
                                    this.logger.LogDebug("Sending for transaction data from peer '{0}'.", peer.RemoteSocketEndpoint);
                                    var filterPayload = new FeeFilterPayload() { NewFeeFilter = filterToSend };
                                    await peer.SendMessageAsync(filterPayload).ConfigureAwait(false);
                                    this.lastSendFilter = filterToSend;
                                }
                            }
                        },
                        this.nodeLifetime.ApplicationStopping,
                        repeatEvery: TimeSpan.FromMinutes(10),
                        startAfter: TimeSpans.TenSeconds);
                    }
                }
            }
        }

        private async Task ProcessMessageAsync(INetworkPeer peer, IncomingMessage message)
        {
            try
            {
                switch (message.Message.Payload)
                {
                    case FeeFilterPayload feeFilter:
                        await this.ProcessFeeFilterAsync(peer, feeFilter).ConfigureAwait(false);
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                this.logger.LogTrace("(-)[CANCELED_EXCEPTION]");
                return;
            }
        }

        private async Task OnMessageReceivedAsync(INetworkPeer peer, IncomingMessage message)
        {
            try
            {
                await this.ProcessMessageAsync(peer, message).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogTrace("(-)[CANCELED_EXCEPTION]");
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogError("Exception occurred: {0}", ex.ToString());
                throw;
            }
        }

        public override object Clone()
        {
            return new FeeFilterBehavior(
                this.network,
                this.settings,
                this.mempoolManager,
                this.initialBlockDownloadState,
                this.loggerFactory,
                this.nodeLifetime,
                this.asyncProvider);
        }

        protected override void AttachCore()
        {
            this.AttachedPeer.StateChanged.Register(this.OnStateChangedAsync);
            this.AttachedPeer.MessageReceived.Register(this.OnMessageReceivedAsync);
        }

        private Task OnStateChangedAsync(INetworkPeer sender, NetworkPeerState arg)
        {
            if (arg == NetworkPeerState.HandShaked)
            {
                this.StartFeeFilterBroadcast(sender);
            }

            if (arg == NetworkPeerState.Disconnecting)
            {
                if (this.asyncLoop != null)
                {
                    this.asyncLoop?.Dispose();
                    this.asyncLoop = null;
                }
            }

            return Task.CompletedTask;
        }

        protected override void DetachCore()
        {
            this.AttachedPeer.StateChanged.Unregister(this.OnStateChangedAsync);
            this.AttachedPeer.MessageReceived.Unregister(this.OnMessageReceivedAsync);
        }
    }
}