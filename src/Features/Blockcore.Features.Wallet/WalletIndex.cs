using System.Collections.Generic;
using NBitcoin;

namespace Blockcore.Features.Wallet
{
    /// <summary>
    /// In order to allow faster look-ups of transactions affecting the wallets' addresses,
    /// we keep a couple of objects in memory.
    /// </summary>
    public class WalletIndex
    {
        // In order to allow faster look-ups of transactions affecting the wallets' addresses,
        // we keep a couple of objects in memory:

        /// <summary>
        /// The list of unspent outputs (utxos) for checking whether inputs from a transaction are being spent by our wallet.
        /// </summary>
        public Dictionary<OutPoint, TransactionData> OutpointLookup { get; internal set; }

        /// <summary>
        /// The list of addresses contained in our wallet for checking whether a transaction is being paid to the wallet.
        /// </summary>
        public ScriptToAddressLookup ScriptToAddressLookup { get; internal set; }

        /// <summary>
        /// A mapping of all inputs with their corresponding transactions, to facilitate rapid lookup.
        /// This index is used for double spend scenarios, if a utxo is spent in a different trx
        /// from what the wallet initially detected this will be found inthois list.
        /// </summary>
        public Dictionary<OutPoint, TransactionData> InputLookup { get; internal set; }

        /// <summary>
        /// A reference to the wallet of the current index.
        /// </summary>
        public Wallet Wallet { get; internal set; }
    }
}