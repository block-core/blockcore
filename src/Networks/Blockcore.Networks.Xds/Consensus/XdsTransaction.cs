using System.IO;
using Blockcore.Consensus.TransactionInfo;

namespace Blockcore.Networks.Xds.Consensus
{
    public class XdsTransaction : Transaction
    {
        public override bool IsProtocolTransaction()
        {
            return this.IsCoinBase || this.IsCoinStake;
        }
    }
}