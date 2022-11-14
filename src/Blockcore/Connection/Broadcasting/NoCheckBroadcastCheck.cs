using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Interfaces;

namespace Blockcore.Connection.Broadcasting
{
    /// <summary>
    /// Broadcast that makes not checks.
    /// </summary>
    public class NoCheckBroadcastCheck : IBroadcastCheck
    {
        public NoCheckBroadcastCheck()
        {
        }

        public Task<string> CheckTransaction(Transaction transaction)
        {
            return Task.FromResult(string.Empty);
        }
    }
}