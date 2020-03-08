﻿using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Interfaces
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
