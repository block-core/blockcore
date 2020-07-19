using System.Collections.Generic;
using Blockcore.Features.Wallet.Database;

namespace Blockcore.Features.Wallet.Types
{
    public class AccountHistory
    {
        /// <summary>
        /// The account for which the history is retrieved.
        /// </summary>
        public HdAccount Account { get; set; }

        /// <summary>
        /// The collection of history items.
        /// </summary>
        public IEnumerable<FlatHistory> History { get; set; }
    }

    /// <summary>
    /// A class that represents a flat view of the wallets history.
    /// </summary>
    public class FlatHistory
    {
        /// <summary>
        /// The address associated with this UTXO.
        /// </summary>
        public HdAddress Address { get; set; }

        /// <summary>
        /// The transaction representing the UTXO.
        /// </summary>
        public TransactionOutputData Transaction { get; set; }
    }

    public class AccountHistorySlim
    {
        /// <summary>
        /// The account for which the history is retrieved.
        /// </summary>
        public HdAccount Account { get; set; }

        /// <summary>
        /// The collection of history items.
        /// </summary>
        public IEnumerable<FlatHistorySlim> History { get; set; }
    }

    /// <summary>
    /// A class that represents a flat view of the wallets history.
    /// </summary>
    public class FlatHistorySlim
    {
        /// <summary>
        /// The address associated with this UTXO.
        /// </summary>
        public HdAddress Address { get; set; }

        /// <summary>
        /// The transaction representing the UTXO.
        /// </summary>
        public WalletHistoryData Transaction { get; set; }
    }
}