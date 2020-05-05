using System.Linq;
using System.Threading.Tasks;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Connection.Broadcasting
{
    /// <summary>
    /// A broadcaster that just pings peers with a trx hash.
    /// This broadcaster cannot perform any checks that the trx is valid (a mempool is needed for that).
    /// </summary>
    public class NodeBroadcasterManager : BroadcasterManagerBase
    {
        public NodeBroadcasterManager(IConnectionManager connectionManager) : base(connectionManager)
        {
        }

        /// <inheritdoc />
        public override async Task BroadcastTransactionAsync(Transaction transaction)
        {
            Guard.NotNull(transaction, nameof(transaction));

            if (this.IsPropagated(transaction))
                return;

            await this.PropagateTransactionToPeersAsync(transaction, this.connectionManager.ConnectedPeers.ToList()).ConfigureAwait(false);
        }
    }
}