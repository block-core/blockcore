using System.Threading.Tasks;
using Blockcore.Consensus.Transaction;
using NBitcoin;

namespace Blockcore.Interfaces
{
    public interface IPooledTransaction
    {
        Task<Transaction> GetTransaction(uint256 trxid);
    }
}
