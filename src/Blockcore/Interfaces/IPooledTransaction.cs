using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.NBitcoin;

namespace Blockcore.Interfaces
{
    public interface IPooledTransaction
    {
        Task<Transaction> GetTransaction(uint256 trxid);
    }
}
