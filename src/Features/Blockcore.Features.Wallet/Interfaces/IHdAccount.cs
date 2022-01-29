using System;
using System.Collections.Generic;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Types;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Features.Wallet.Interfaces
{
    public interface IHdAccount
    {
        DateTimeOffset CreationTime { get; set; }
        string ExtendedPubKey { get; set; }
        ICollection<HdAddress> ExternalAddresses { get; set; }
        string HdPath { get; set; }
        int Index { get; set; }
        ICollection<HdAddress> InternalAddresses { get; set; }
        string Name { get; set; }

        IEnumerable<HdAddress> CreateAddresses(Network network, int addressesQuantity, bool isChange = false);
        (Money ConfirmedAmount, Money UnConfirmedAmount) GetBalances(IWalletStore walletStore, bool excludeColdStakeUtxo);
        int GetCoinType();
        IEnumerable<HdAddress> GetCombinedAddresses();
        HdAddress GetFirstUnusedChangeAddress(IWalletStore walletStore);
        HdAddress GetFirstUnusedReceivingAddress(IWalletStore walletStore);
        HdAddress GetLastUsedAddress(IWalletStore walletStore, bool isChange);
        IEnumerable<UnspentOutputReference> GetSpendableTransactions(IWalletStore walletStore, int currentChainHeight, long coinbaseMaturity, int confirmations = 0);
        bool IsNormalAccount();
    }
}