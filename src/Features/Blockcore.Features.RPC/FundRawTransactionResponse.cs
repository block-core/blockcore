using Blockcore.Consensus.TransactionInfo;
using Blockcore.NBitcoin;

namespace Blockcore.Features.RPC
{
    public class FundRawTransactionResponse
    {
        public Transaction Transaction
        {
            get; set;
        }
        public Money Fee
        {
            get; set;
        }
        public int ChangePos
        {
            get; set;
        }
    }
}
