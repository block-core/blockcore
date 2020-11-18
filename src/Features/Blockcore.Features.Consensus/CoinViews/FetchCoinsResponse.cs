using System.Collections.Generic;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Utilities;
using NBitcoin;

namespace Blockcore.Features.Consensus.CoinViews
{
    /// <summary>
    /// Return value of <see cref="CoinView.FetchCoinsAsync(OutPoint[])"/>,
    /// contains the coinview tip's hash and information about unspent coins in the requested transactions.
    /// </summary>
    public class FetchCoinsResponse
    {
        /// <summary>Unspent outputs of the requested transactions.</summary>
        public Dictionary<OutPoint, UnspentOutput> UnspentOutputs { get; private set; }

        public FetchCoinsResponse()
        {
            this.UnspentOutputs = new Dictionary<OutPoint, UnspentOutput>();
        }
    }
}
