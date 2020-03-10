using System.Threading.Tasks;
using NBitcoin;

namespace Blockcore.Interfaces
{
    public interface IPooledTransaction
    {
        Task<Transaction> GetTransaction(uint256 trxid);
    }
}
