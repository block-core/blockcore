using System.Collections.Generic;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.Wallet.Types;
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
        public Dictionary<OutPoint, string> OutputLookup { get; internal set; }

        /// <summary>
        /// The list of addresses contained in our wallet for checking whether a transaction is being paid to the wallet.
        /// </summary>
        public ScriptToAddressLookup ScriptToAddressLookup { get; internal set; }

        /// <summary>
        /// A mapping of all inputs to an output of our wallet, to facilitate rapid lookup.
        /// This index is used for double spend scenarios, if a utxo is spent in a different trx
        /// from what the wallet initially detected this will be found in this list.
        /// </summary>
        public Dictionary<OutPoint, OutPoint> InputLookup { get; internal set; }

        /// <summary>
        /// A reference to the wallet of the current index.
        /// </summary>
        public Types.Wallet Wallet { get; internal set; }
    }
}