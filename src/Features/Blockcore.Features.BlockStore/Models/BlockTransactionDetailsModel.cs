using System.Linq;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Controllers.Models;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Features.BlockStore.Models
{
    public class BlockTransactionDetailsModel : BlockModel
    {
        /// <summary>
        /// Hides the existing Transactions property of type <see cref="string[]"/> and replaces with the <see cref="TransactionVerboseModel[]"/>.
        /// </summary>
        public new TransactionVerboseModel[] Transactions { get; set; }

        public BlockTransactionDetailsModel(Block block, ChainedHeader chainedHeader, ChainedHeader tip, Network network) : base(block, chainedHeader, tip, network)
        {
            this.Transactions = block.Transactions.Select(trx => new TransactionVerboseModel(trx, network)).ToArray();
        }

        public BlockTransactionDetailsModel()
        {
        }
    }
}
