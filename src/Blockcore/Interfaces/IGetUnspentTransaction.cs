using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Interfaces
{
    /// <summary>
    /// An interface used to retrieve unspent transactions
    /// </summary>
    public interface IGetUnspentTransaction
    {
        /// <summary>
        /// Returns the unspent output for a specific transaction.
        /// </summary>
        /// <param name="outPoint">Hash of the transaction to query.</param>
        /// <returns>Unspent Output</returns>
        Task<UnspentOutput> GetUnspentTransactionAsync(OutPoint outPoint);
    }
}
