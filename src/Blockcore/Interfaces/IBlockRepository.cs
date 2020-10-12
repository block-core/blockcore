using System.Threading.Tasks;
using Blockcore.Consensus.Block;
using NBitcoin;

namespace Blockcore.Interfaces
{
    public interface INBitcoinBlockRepository
    {
        Task<Block> GetBlockAsync(uint256 blockId);
    }

    public interface IBlockTransactionMapStore
    {
        uint256 GetBlockHash(uint256 trxHash);
    }
}
