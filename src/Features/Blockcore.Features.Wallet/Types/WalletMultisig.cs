using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Features.Wallet.Helpers;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Networks;
using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Types
{
    public class WalletMultisig : Wallet
    {
        public WalletMultisig()
        {
            this.IsMultisig = true;
            this.AccountsRoot = new List<IAccountRoot>();
        }

        public WalletMultisig(Network network):this()
        {
            this.Network = network;            
        }

        public WalletMultisig(string walletName, string encryptedSeed, byte[] chainCode, Network network):this()
        {
            this.Name = walletName;
            this.EncryptedSeed = encryptedSeed;
            this.ChainCode = chainCode;
            this.Network = network;
        }

        /// <summary>
        /// The root of the accounts tree.
        /// </summary>
        [JsonConverter(typeof(AccountRootMultisigConverter))]
        [JsonProperty(PropertyName = "accountsRoot")]
        public override ICollection<IAccountRoot> AccountsRoot { get; set; }
        /// <summary>
        /// Adds an account to the current wallet.
        /// </summary>
        /// <remarks>
        /// The name given to the account is of the form "account (i)" by default, where (i) is an incremental index starting at 0.
        /// According to BIP44, an account at index (i) can only be created when the account at index (i - 1) contains at least one transaction.
        /// </remarks>
        /// <seealso cref="https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki"/>
        /// <param name="password">The password used to decrypt the wallet's <see cref="EncryptedSeed"/>.</param>
        /// <param name="coinType">The type of coin this account is for.</param>
        /// <param name="accountCreationTime">Creation time of the account to be created.</param>
        /// <returns>A new HD account.</returns>
        public HdAccountMultisig AddNewAccount(MultisigScheme scheme, int coinType, DateTimeOffset accountCreationTime)
        {
            return AddNewAccount(scheme, coinType, this.Network, accountCreationTime);
        }

        /// <summary>
        /// Creates an account as expected in bip-44 account structure.
        /// </summary>
        /// <param name="chainCode"></param>
        /// <param name="network"></param>
        /// <param name="accountCreationTime"></param>
        /// <returns></returns>
        public HdAccountMultisig AddNewAccount(MultisigScheme multisigScheme, int coinType, Network network, DateTimeOffset accountCreationTime)
        {
            // Get the current collection of accounts.
            var accounts = this.AccountsRoot.FirstOrDefault().Accounts;
            this.AccountsRoot.FirstOrDefault().LastBlockSyncedHash = network.GenesisHash;
            this.AccountsRoot.FirstOrDefault().LastBlockSyncedHeight = 0;

            int newAccountIndex = 0;
            if (accounts.Any())
            {
                newAccountIndex = accounts.Max(a => a.Index) + 1;
            }
            
            string accountHdPath = $"m/44'/{(int)coinType}'/{newAccountIndex}'";

            var newAccount = new HdAccountMultisig(multisigScheme)
            {
                Index = newAccountIndex,
                ExternalAddresses = new List<HdAddress>(),
                InternalAddresses = new List<HdAddress>(),
                Name = $"account {newAccountIndex}",
                HdPath = accountHdPath,
                CreationTime = accountCreationTime
            };

            accounts.Add(newAccount);

            return newAccount;
        }

    }

    /// <summary>
    /// Provides owerrden mothods for account creation in multisig wallet.
    /// </summary>
    public class AccountRootMultisig : AccountRoot, IAccountRoot
    {
        public AccountRootMultisig(List<HdAccountMultisig> accounts, int? coinType, uint256 lastBlockSyncedHash, int? lastBlockSyncedHeight)
        {
            this.Accounts = (ICollection<IHdAccount>)accounts;
            this.CoinType = coinType;
            this.LastBlockSyncedHash = lastBlockSyncedHash;
            this.LastBlockSyncedHeight = lastBlockSyncedHeight;
        }
       
        public AccountRootMultisig()
        {
            this.Accounts = new List<IHdAccount>();
        }

        [JsonConverter(typeof(HdAccountMultisigConverter))]
        [JsonProperty(PropertyName = "accounts")]
        public override ICollection<IHdAccount> Accounts { get; set; }
    }

    public class AccountRootMultisigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(ICollection<IAccountRoot>))
            {
                List<IAccountRoot> accountRoots = new List<IAccountRoot>();
                var list = serializer.Deserialize<List<AccountRootMultisig>>(reader);

                foreach (var item in list)
                {
                    accountRoots.Add(item);
                }
                return accountRoots;
            }
         
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public class HdAccountMultisigConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(ICollection<IHdAccount>))
            {
                List<IHdAccount> accounts = new List<IHdAccount>();
                var list = serializer.Deserialize<List<HdAccountMultisig>>(reader);
                foreach (var item in list)
                {
                    accounts.Add(item);
                }
                return accounts;
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //use the default serialization - it works fine
            serializer.Serialize(writer, value);
        }
    }
    public class HdAccountMultisig : HdAccount, IHdAccount
    {
        public HdAccountMultisig(MultisigScheme scheme)
        {
            this.ExtendedPubKey = "N/A";
            this.MultisigScheme = scheme;
        }      

        [JsonProperty(PropertyName = "multisigScheme")]
        public MultisigScheme MultisigScheme { get; set; }
        /// Generates an HD public key derived from an extended public key.
        /// </summary>
        /// <param name="accountExtPubKey">The extended public key used to generate child keys.</param>
        /// <param name="index">The index of the child key to generate.</param>
        /// <param name="isChange">A value indicating whether the public key to generate corresponds to a change address.</param>
        /// <returns>
        /// An HD public key derived from an extended public key.
        /// </returns>

        public Script GeneratePublicKey(int hdPathIndex, Network network, bool isChange = false)
        {
            List<PubKey> derivedPubKeys = new List<PubKey>();
            foreach (var xpub in this.MultisigScheme.XPubs)
            {
                derivedPubKeys.Add(HdOperations.GeneratePublicKey(xpub, hdPathIndex, isChange, network));
            }
            var sortedkeys = LexographicalSort(derivedPubKeys);

            Script redeemScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(this.MultisigScheme.Threashold, sortedkeys.ToArray());
            return redeemScript;
        }



        /// <summary>
        /// Creates a number of additional addresses in the current account.
        /// </summary>
        /// <remarks>
        /// The name given to the account is of the form "account (i)" by default, where (i) is an incremental index starting at 0.
        /// According to BIP44, an account at index (i) can only be created when the account at index (i - 1) contains at least one transaction.
        /// </remarks>
        /// <param name="wallet">Instance of a multisig wallet that allows access to multisig scheme, pubkeys threashold and etc</param>
        /// <param name="addressesQuantity">The number of addresses to create.</param>
        /// <param name="isChange">Whether the addresses added are change (internal) addresses or receiving (external) addresses.</param>
        /// <returns>The created addresses.</returns>
        public override IEnumerable<HdAddress> CreateAddresses(Network network, int addressesQuantity, bool isChange = false)
        {
            var addresses = isChange ? this.InternalAddresses : this.ExternalAddresses;

            // Get the index of the last address.
            int firstNewAddressIndex = 0;
            if (addresses.Any())
            {
                firstNewAddressIndex = addresses.Max(add => add.Index) + 1;
            }

            List<HdAddress> addressesCreated = new List<HdAddress>();
            for (int i = firstNewAddressIndex; i < firstNewAddressIndex + addressesQuantity; i++)
            {
                // Generate a new address.                
                var pubkey = GeneratePublicKey(i, network, isChange);
                BitcoinAddress address = pubkey.Hash.GetAddress(network);
                // Add the new address details to the list of addresses.
                HdAddress newAddress = new HdAddress
                {
                    Index = i,
                    HdPath = CreateHdPath((int)this.GetCoinType(), this.Index, i, isChange),
                    ScriptPubKey = address.ScriptPubKey,
                    Pubkey = pubkey,
                    Address = address.ToString(),
                    RedeemScript = pubkey
                };

                addresses.Add(newAddress);
                addressesCreated.Add(newAddress);
            }

            if (isChange)
            {
                this.InternalAddresses = addresses;
            }
            else
            {
                this.ExternalAddresses = addresses;
            }

            return addressesCreated;
        }

        public static string CreateHdPath(int coinType, int accountIndex, int addressIndex, bool isChange = false)
        {
            int change = isChange ? 1 : 0;
            return $"m/44'/{coinType}'/{accountIndex}'/{change}/{addressIndex}";
        }

        private static IEnumerable<PubKey> LexographicalSort(IEnumerable<PubKey> pubKeys)
        {
            List<PubKey> list = new List<PubKey>();
            var e = pubKeys.Select(s => s.ToHex());
            var sorted = e.OrderByDescending(s => s.Length).ThenBy(r => r);
            foreach (var item in sorted)
            {
                list.Add(new PubKey(item));
            }
            return list;
        }
    }
}

