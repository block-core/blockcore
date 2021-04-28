using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using Blockcore.AsyncWork;
using Blockcore.Configuration;
using Blockcore.Connection.Broadcasting;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.EventBus;
using Blockcore.Features.BlockStore;
using Blockcore.Features.BlockStore.Models;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Features.Wallet.Helpers;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Features.Wallet.Types;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Signals;
using Blockcore.Utilities;
using Blockcore.Utilities.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BuilderExtensions;
using NBitcoin.Policy;

[assembly: InternalsVisibleTo("Blockcore.Features.Wallet.Tests")]

namespace Blockcore.Features.Wallet
{
    /// <summary>
    /// A manager providing operations on wallets.
    /// </summary>
    public class WalletManager : IWalletManager
    {
        /// <summary>Used to get the first account.</summary>
        public const string DefaultAccount = "account 0";

        // <summary>As per RPC method definition this should be the max allowable expiry duration.</summary>
        private const int MaxWalletUnlockDurationInSeconds = 1073741824;

        /// <summary>Quantity of accounts created in a wallet file when a wallet is restored.</summary>
        private const int WalletRecoveryAccountsCount = 1;

        /// <summary>Quantity of accounts created in a wallet file when a wallet is created.</summary>
        private const int WalletCreationAccountsCount = 1;

        /// <summary>File extension for wallet files.</summary>
        private const string WalletFileExtension = "wallet.json";

        /// <summary>Timer for saving wallet files to the file system.</summary>
        private const int WalletSavetimeIntervalInMinutes = 5;

        private const string DownloadChainLoop = "WalletManager.DownloadChain";

        /// <summary>
        /// A lock object that protects access to the <see cref="Wallet"/>.
        /// Any of the collections inside Wallet must be synchronized using this lock.
        /// </summary>
        protected readonly object lockObject;

        /// <summary>The async loop we need to wait upon before we can shut down this manager.</summary>
        private IAsyncLoop asyncLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncProvider asyncProvider;

        /// <summary>Gets the list of wallets.</summary>
        public ConcurrentBag<Types.Wallet> Wallets { get; }

        /// <summary>The type of coin used in this manager.</summary>
        protected readonly int coinType;

        /// <summary>Specification of the network the node runs on - regtest/testnet/mainnet.</summary>
        protected readonly Network network;

        /// <summary>The chain of headers.</summary>
        protected readonly ChainIndexer ChainIndexer;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly INodeLifetime nodeLifetime;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>An object capable of storing <see cref="Wallet"/>s to the file system.</summary>
        private readonly FileStorage<Types.Wallet> fileStorage;

        /// <summary>The broadcast manager.</summary>
        private readonly IBroadcasterManager broadcasterManager;

        /// <summary>Provider of time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Utxo Indexer.</summary>
        private readonly IUtxoIndexer utxoIndexer;

        /// <summary>Policy for wallet fees</summary>
        private readonly IWalletFeePolicy walletFeePolicy;

        /// <summary>The settings for the wallet feature.</summary>
        private readonly WalletSettings walletSettings;

        private readonly DataFolder dataFolder;

        /// <summary>The settings for the wallet feature.</summary>
        private readonly IScriptAddressReader scriptAddressReader;

        /// <summary>The private key cache for unlocked wallets.</summary>
        private readonly MemoryCache privateKeyCache;

        private readonly ISignals signals;

        public uint256 WalletTipHash { get; set; }

        public int WalletTipHeight { get; set; }

        private SubscriptionToken broadcastTransactionStateChanged;

        protected internal Dictionary<string, WalletIndex> walletIndex;

        public WalletManager(
            ILoggerFactory loggerFactory,
            Network network,
            ChainIndexer chainIndexer,
            WalletSettings walletSettings,
            DataFolder dataFolder,
            IWalletFeePolicy walletFeePolicy,
            IAsyncProvider asyncProvider,
            INodeLifetime nodeLifetime,
            IDateTimeProvider dateTimeProvider,
            IScriptAddressReader scriptAddressReader,
            ISignals signals = null,
            IBroadcasterManager broadcasterManager = null, // no need to know about transactions the node will broadcast to.
            IUtxoIndexer utxoIndexer = null)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(network, nameof(network));
            Guard.NotNull(chainIndexer, nameof(chainIndexer));
            Guard.NotNull(walletSettings, nameof(walletSettings));
            Guard.NotNull(dataFolder, nameof(dataFolder));
            Guard.NotNull(walletFeePolicy, nameof(walletFeePolicy));
            Guard.NotNull(asyncProvider, nameof(asyncProvider));
            Guard.NotNull(nodeLifetime, nameof(nodeLifetime));
            Guard.NotNull(scriptAddressReader, nameof(scriptAddressReader));

            this.walletSettings = walletSettings;
            this.dataFolder = dataFolder;
            this.lockObject = new object();

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.Wallets = new ConcurrentBag<Types.Wallet>();

            this.network = network;
            this.coinType = network.Consensus.CoinType;
            this.ChainIndexer = chainIndexer;
            this.asyncProvider = asyncProvider;
            this.nodeLifetime = nodeLifetime;
            this.fileStorage = new FileStorage<Types.Wallet>(dataFolder.WalletPath);
            this.signals = signals;
            this.broadcasterManager = broadcasterManager;
            this.scriptAddressReader = scriptAddressReader;
            this.dateTimeProvider = dateTimeProvider;
            this.utxoIndexer = utxoIndexer;
            this.walletFeePolicy = walletFeePolicy;

            // register events
            if (this.signals != null)
            {
                this.broadcastTransactionStateChanged = this.signals.Subscribe<TransactionBroadcastEvent>(this.BroadcasterTransactionStateChanged);
            }

            this.walletIndex = new Dictionary<string, WalletIndex>();

            this.privateKeyCache = new MemoryCache(new MemoryCacheOptions() { ExpirationScanFrequency = new TimeSpan(0, 1, 0) });
        }

        /// <summary>
        /// Creates the <see cref="ScriptToAddressLookup"/> object to use.
        /// </summary>
        /// <remarks>
        /// Override this method and the <see cref="ScriptToAddressLookup"/> object to provide a custom keys lookup.
        /// </remarks>
        /// <returns>A new <see cref="ScriptToAddressLookup"/> object for use by this class.</returns>
        protected virtual ScriptToAddressLookup CreateAddressFromScriptLookup()
        {
            return new ScriptToAddressLookup();
        }

        /// <inheritdoc />
        public virtual Dictionary<string, ScriptTemplate> GetValidStakingTemplates()
        {
            return new Dictionary<string, ScriptTemplate> {
                { "P2PK", PayToPubkeyTemplate.Instance },
                { "P2PKH", PayToPubkeyHashTemplate.Instance },
                { "P2SH", PayToScriptHashTemplate.Instance },
                { "P2WPKH", PayToWitPubKeyHashTemplate.Instance },
                { "P2WSH", PayToWitScriptHashTemplate.Instance }
            };
        }

        // <inheritdoc />
        public virtual IEnumerable<BuilderExtension> GetTransactionBuilderExtensionsForStaking()
        {
            return new List<BuilderExtension>();
        }

        private void BroadcasterTransactionStateChanged(TransactionBroadcastEvent broadcastEntry)
        {
            if (string.IsNullOrEmpty(broadcastEntry.BroadcastEntry.ErrorMessage))
            {
                this.ProcessTransaction(broadcastEntry.BroadcastEntry.Transaction, null, null, broadcastEntry.BroadcastEntry.TransactionBroadcastState == TransactionBroadcastState.Propagated);
            }
            else
            {
                this.logger.LogDebug("Exception occurred: {0}", broadcastEntry.BroadcastEntry.ErrorMessage);
                this.logger.LogTrace("(-)[EXCEPTION]");
            }
        }

        public void Start()
        {
            this.fileStorage.CloneLegacyWallet(WalletFileExtension);

            // Find wallets and load them in memory.
            IEnumerable<Types.Wallet> wallets = this.fileStorage.LoadByFileExtension(WalletFileExtension);

            foreach (Types.Wallet wallet in wallets)
            {
                this.Load(wallet);
                foreach (HdAccount account in wallet.GetAccounts())
                {
                    this.AddAddressesToMaintainBuffer(wallet, account, false);
                    this.AddAddressesToMaintainBuffer(wallet, account, true);
                }
            }

            if (this.walletSettings.IsDefaultWalletEnabled())
            {
                // Check if it already exists, if not, create one.
                if (!wallets.Any(w => w.Name == this.walletSettings.DefaultWalletName))
                {
                    var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                    this.CreateWallet(this.walletSettings.DefaultWalletPassword, this.walletSettings.DefaultWalletName, string.Empty, mnemonic);
                }

                // Make sure both unlock is specified, and that we actually have a default wallet name specified.
                if (this.walletSettings.UnlockDefaultWallet)
                {
                    this.UnlockWallet(this.walletSettings.DefaultWalletPassword, this.walletSettings.DefaultWalletName, MaxWalletUnlockDurationInSeconds);
                }
            }

            // Load data in memory for faster lookups.
            this.LoadKeysLookup();

            // Find the last chain block received by the wallet manager.
            HashHeightPair hashHeightPair = this.LastReceivedBlockInfo();
            this.WalletTipHash = hashHeightPair.Hash;
            this.WalletTipHeight = hashHeightPair.Height;

            // Save the wallets file every 5 minutes to help against crashes.
            this.asyncLoop = this.asyncProvider.CreateAndRunAsyncLoop("Wallet persist job", token =>
            {
                this.SaveWallets();
                this.logger.LogInformation("Wallets saved to file at {0}.", this.dateTimeProvider.GetUtcNow());

                this.logger.LogTrace("(-)[IN_ASYNC_LOOP]");
                return Task.CompletedTask;
            },
            this.nodeLifetime.ApplicationStopping,
            repeatEvery: TimeSpan.FromMinutes(WalletSavetimeIntervalInMinutes),
            startAfter: TimeSpan.FromMinutes(WalletSavetimeIntervalInMinutes));
        }

        /// <inheritdoc />
        public void Stop()
        {
            this.asyncLoop?.Dispose();
            this.SaveWallets();
            foreach (IDisposable disposable in this.Wallets.Select(s => s.walletStore).OfType<IDisposable>())
                disposable?.Dispose();
        }

        /// <inheritdoc />
        public Mnemonic CreateWallet(string password, string name, string passphrase, Mnemonic mnemonic = null, int? coinType = null)
        {
            Guard.NotEmpty(password, nameof(password));
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(passphrase, nameof(passphrase));

            // Generate the root seed used to generate keys from a mnemonic picked at random
            // and a passphrase optionally provided by the user.
            mnemonic = mnemonic ?? new Mnemonic(Wordlist.English, WordCount.Twelve);

            ExtKey extendedKey = HdOperations.GetExtendedKey(mnemonic, passphrase);

            // Create a wallet file.
            string encryptedSeed = extendedKey.PrivateKey.GetEncryptedBitcoinSecret(password, this.network).ToWif();
            Types.Wallet wallet = this.GenerateWalletFile(name, encryptedSeed, extendedKey.ChainCode, coinType: coinType);

            // Generate multiple accounts and addresses from the get-go.
            for (int i = 0; i < WalletCreationAccountsCount; i++)
            {
                HdAccount account = wallet.AddNewAccount(password, this.dateTimeProvider.GetTimeOffset());
                IEnumerable<HdAddress> newReceivingAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer);
                IEnumerable<HdAddress> newChangeAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer, true);
                this.UpdateKeysLookup(wallet, newReceivingAddresses.Concat(newChangeAddresses));
            }

            // If the chain is downloaded, we set the height of the newly created wallet to it.
            // However, if the chain is still downloading when the user creates a wallet,
            // we wait until it is downloaded in order to set it. Otherwise, the height of the wallet will be the height of the chain at that moment.
            if (this.ChainIndexer.IsDownloaded())
            {
                this.UpdateLastBlockSyncedHeight(wallet, this.ChainIndexer.Tip);
            }
            else
            {
                this.UpdateWhenChainDownloaded(new[] { wallet }, this.dateTimeProvider.GetUtcNow());
            }

            // Save the changes to the file and add addresses to be tracked.
            this.SaveWallet(wallet);
            this.Load(wallet);

            return mnemonic;
        }

        /// <inheritdoc />
        public string RetrievePrivateKey(string password, string walletName, string accountName, string address)
        {
            Guard.NotEmpty(password, nameof(password));
            Guard.NotEmpty(walletName, nameof(walletName));
            Guard.NotEmpty(address, nameof(address));

            Types.Wallet wallet = this.GetWallet(walletName);

            // Locate the address based on its base58 string representation.
            HdAddress hdAddress = wallet.GetAddress(address, account => account.Name.Equals(accountName));

            ISecret privateKey = wallet.GetExtendedPrivateKeyForAddress(password, hdAddress).PrivateKey.GetWif(this.network);
            return privateKey.ToString();
        }

        /// <inheritdoc />
        public SignMessageResult SignMessage(string password, string walletName, string accountName, string externalAddress, string message)
        {
            Guard.NotEmpty(password, nameof(password));
            Guard.NotEmpty(walletName, nameof(walletName));
            Guard.NotEmpty(accountName, nameof(accountName));
            Guard.NotEmpty(message, nameof(message));
            Guard.NotEmpty(externalAddress, nameof(externalAddress));

            // Get wallet
            Types.Wallet wallet = this.GetWalletByName(walletName);

            // Sign the message.
            HdAddress hdAddress = wallet.GetAddress(externalAddress, account => account.Name.Equals(accountName));
            Key privateKey = wallet.GetExtendedPrivateKeyForAddress(password, hdAddress).PrivateKey;
            return new SignMessageResult()
            {
                Signature = privateKey.SignMessage(message),
                SignedAddress = hdAddress.Address
            };
        }

        /// <inheritdoc />
        public bool VerifySignedMessage(string externalAddress, string message, string signature)
        {
            Guard.NotEmpty(message, nameof(message));
            Guard.NotEmpty(externalAddress, nameof(externalAddress));
            Guard.NotEmpty(signature, nameof(signature));

            bool result = false;

            try
            {
                BitcoinPubKeyAddress bitcoinPubKeyAddress = new BitcoinPubKeyAddress(externalAddress, this.network);
                result = bitcoinPubKeyAddress.VerifyMessage(message, signature);
            }
            catch (Exception ex)
            {
                this.logger.LogDebug("Failed to verify message: {0}", ex.ToString());
                this.logger.LogTrace("(-)[EXCEPTION]");
            }
            return result;
        }

        /// <inheritdoc />
        public Types.Wallet LoadWallet(string password, string name)
        {
            Guard.NotEmpty(password, nameof(password));
            Guard.NotEmpty(name, nameof(name));

            // Load the file from the local system.
            Types.Wallet wallet = this.fileStorage.LoadByFileName($"{name}.{WalletFileExtension}");

            // Check the password.
            try
            {
                if (!wallet.IsExtPubKeyWallet)
                    Key.Parse(wallet.EncryptedSeed, password, wallet.Network);
            }
            catch (Exception ex)
            {
                this.logger.LogDebug("Exception occurred: {0}", ex.ToString());
                this.logger.LogTrace("(-)[EXCEPTION]");
                throw new SecurityException(ex.Message);
            }

            this.Load(wallet);
            this.LoadKeysLookup();

            return wallet;
        }

        /// <inheritdoc />
        public void UnlockWallet(string password, string name, int timeout)
        {
            Guard.NotEmpty(password, nameof(password));
            Guard.NotEmpty(name, nameof(name));

            // Length of expiry of the unlocking, restricted to max duration.
            TimeSpan duration = new TimeSpan(0, 0, Math.Min(timeout, MaxWalletUnlockDurationInSeconds));

            this.CacheSecret(name, password, duration);
        }

        /// <inheritdoc />
        public void LockWallet(string name)
        {
            Guard.NotNull(name, nameof(name));

            Types.Wallet wallet = this.GetWalletByName(name);
            string cacheKey = wallet.EncryptedSeed;
            this.privateKeyCache.Remove(cacheKey);
        }

        private SecureString CacheSecret(string name, string walletPassword, TimeSpan duration)
        {
            Types.Wallet wallet = this.GetWalletByName(name);
            string cacheKey = wallet.EncryptedSeed;

            if (!this.privateKeyCache.TryGetValue(cacheKey, out SecureString secretValue))
            {
                Key privateKey = Key.Parse(wallet.EncryptedSeed, walletPassword, wallet.Network);
                secretValue = privateKey.ToString(wallet.Network).ToSecureString();
            }

            this.privateKeyCache.Set(cacheKey, secretValue, duration);

            return secretValue;
        }

        /// <inheritdoc />
        public virtual Types.Wallet RecoverWallet(string password, string name, string mnemonic, DateTime creationTime, string passphrase, int? coinType = null)
        {
            Guard.NotEmpty(password, nameof(password));
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(mnemonic, nameof(mnemonic));
            Guard.NotNull(passphrase, nameof(passphrase));

            // Generate the root seed used to generate keys.
            ExtKey extendedKey;
            try
            {
                extendedKey = HdOperations.GetExtendedKey(mnemonic, passphrase);
            }
            catch (NotSupportedException ex)
            {
                this.logger.LogDebug("Exception occurred: {0}", ex.ToString());
                this.logger.LogTrace("(-)[EXCEPTION]");

                if (ex.Message == "Unknown")
                    throw new WalletException("Please make sure you enter valid mnemonic words.");

                throw;
            }

            // Create a wallet file.
            string encryptedSeed = extendedKey.PrivateKey.GetEncryptedBitcoinSecret(password, this.network).ToWif();
            Types.Wallet wallet = this.GenerateWalletFile(name, encryptedSeed, extendedKey.ChainCode, creationTime, coinType);

            // Generate multiple accounts and addresses from the get-go.
            for (int i = 0; i < WalletRecoveryAccountsCount; i++)
            {
                HdAccount account;
                lock (this.lockObject)
                {
                    account = wallet.AddNewAccount(password, this.dateTimeProvider.GetTimeOffset());
                }

                IEnumerable<HdAddress> newReceivingAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer);
                IEnumerable<HdAddress> newChangeAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer, true);
                this.UpdateKeysLookup(wallet, newReceivingAddresses.Concat(newChangeAddresses));
            }

            // If the chain is downloaded, we set the height of the recovered wallet to that of the recovery date.
            // However, if the chain is still downloading when the user restores a wallet,
            // we wait until it is downloaded in order to set it. Otherwise, the height of the wallet may not be known.
            if (this.ChainIndexer.IsDownloaded())
            {
                int blockSyncStart = this.ChainIndexer.GetHeightAtTime(creationTime);
                this.UpdateLastBlockSyncedHeight(wallet, this.ChainIndexer.GetHeader(blockSyncStart));
            }
            else
            {
                this.UpdateWhenChainDownloaded(new[] { wallet }, creationTime);
            }

            this.SaveWallet(wallet);
            this.Load(wallet);

            return wallet;
        }

        /// <inheritdoc />
        public Types.Wallet RecoverWallet(string name, ExtPubKey extPubKey, int accountIndex, DateTime creationTime)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotNull(extPubKey, nameof(extPubKey));
            this.logger.LogDebug("({0}:'{1}',{2}:'{3}',{4}:'{5}')", nameof(name), name, nameof(extPubKey), extPubKey, nameof(accountIndex), accountIndex);

            // Create a wallet file.
            Types.Wallet wallet = this.GenerateExtPubKeyOnlyWalletFile(name, creationTime);

            // Generate account
            HdAccount account;
            lock (this.lockObject)
            {
                account = wallet.AddNewAccount(extPubKey, accountIndex, this.dateTimeProvider.GetTimeOffset());
            }

            IEnumerable<HdAddress> newReceivingAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer);
            IEnumerable<HdAddress> newChangeAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer, true);
            this.UpdateKeysLookup(wallet, newReceivingAddresses.Concat(newChangeAddresses));

            // If the chain is downloaded, we set the height of the recovered wallet to that of the recovery date.
            // However, if the chain is still downloading when the user restores a wallet,
            // we wait until it is downloaded in order to set it. Otherwise, the height of the wallet may not be known.
            if (this.ChainIndexer.IsDownloaded())
            {
                int blockSyncStart = this.ChainIndexer.GetHeightAtTime(creationTime);
                this.UpdateLastBlockSyncedHeight(wallet, this.ChainIndexer.GetHeader(blockSyncStart));
            }
            else
            {
                this.UpdateWhenChainDownloaded(new[] { wallet }, creationTime);
            }

            // Save the changes to the file and add addresses to be tracked.
            this.SaveWallet(wallet);
            this.Load(wallet);
            return wallet;
        }

        /// <inheritdoc />
        public HdAccount GetUnusedAccount(string walletName, string password)
        {
            Guard.NotEmpty(walletName, nameof(walletName));
            Guard.NotEmpty(password, nameof(password));

            Types.Wallet wallet = this.GetWalletByName(walletName);

            if (wallet.IsExtPubKeyWallet)
            {
                this.logger.LogTrace("(-)[CANNOT_ADD_ACCOUNT_TO_EXTPUBKEY_WALLET]");
                throw new CannotAddAccountToXpubKeyWalletException("Use recover-via-extpubkey instead.");
            }

            HdAccount res = this.GetUnusedAccount(wallet, password);
            return res;
        }

        /// <inheritdoc />
        public HdAccount GetUnusedAccount(Types.Wallet wallet, string password)
        {
            Guard.NotNull(wallet, nameof(wallet));
            Guard.NotEmpty(password, nameof(password));

            HdAccount account;

            lock (this.lockObject)
            {
                account = wallet.GetFirstUnusedAccount(wallet.walletStore);

                if (account != null)
                {
                    this.logger.LogTrace("(-)[ACCOUNT_FOUND]");
                    return account;
                }

                // No unused account was found, create a new one.
                account = wallet.AddNewAccount(password, this.dateTimeProvider.GetTimeOffset());
                IEnumerable<HdAddress> newReceivingAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer);
                IEnumerable<HdAddress> newChangeAddresses = account.CreateAddresses(this.network, this.walletSettings.UnusedAddressesBuffer, true);
                this.UpdateKeysLookup(wallet, newReceivingAddresses.Concat(newChangeAddresses));
            }

            // Save the changes to the file.
            this.SaveWallet(wallet);

            return account;
        }

        public string GetExtPubKey(WalletAccountReference accountReference)
        {
            Guard.NotNull(accountReference, nameof(accountReference));

            Types.Wallet wallet = this.GetWalletByName(accountReference.WalletName);

            string extPubKey;
            lock (this.lockObject)
            {
                // Get the account.
                HdAccount account = wallet.GetAccount(accountReference.AccountName);
                if (account == null)
                    throw new WalletException($"No account with the name '{accountReference.AccountName}' could be found.");
                extPubKey = account.ExtendedPubKey;
            }

            return extPubKey;
        }

        /// <inheritdoc />
        public HdAddress GetUnusedAddress(WalletAccountReference accountReference)
        {
            HdAddress res = this.GetUnusedAddresses(accountReference, 1).Single();

            return res;
        }

        /// <inheritdoc />
        public HdAddress GetUnusedChangeAddress(WalletAccountReference accountReference)
        {
            HdAddress res = this.GetUnusedAddresses(accountReference, 1, true).Single();

            return res;
        }

        /// <inheritdoc />
        public IEnumerable<HdAddress> GetUnusedAddresses(WalletAccountReference accountReference, int count, bool isChange = false, bool alwaysnew = false)
        {
            Guard.NotNull(accountReference, nameof(accountReference));
            Guard.Assert(count > 0);

            Types.Wallet wallet = this.GetWalletByName(accountReference.WalletName);

            bool generated = false;
            IEnumerable<HdAddress> addresses;

            var newAddresses = new List<HdAddress>();

            lock (this.lockObject)
            {
                // Get the account.
                HdAccount account = wallet.GetAccount(accountReference.AccountName);
                if (account == null)
                    throw new WalletException($"No account with the name '{accountReference.AccountName}' could be found.");

                List<HdAddress> unusedAddresses = isChange ?
                    account.InternalAddresses.Where(acc => wallet.walletStore.CountForAddress(acc.Address) == 0).ToList() :
                    account.ExternalAddresses.Where(acc => wallet.walletStore.CountForAddress(acc.Address) == 0).ToList();

                int diff = alwaysnew ? -1 : unusedAddresses.Count - count;

                if (diff < 0)
                {
                    newAddresses = account.CreateAddresses(this.network, Math.Abs(diff), isChange: isChange).ToList();
                    this.UpdateKeysLookup(wallet, newAddresses);
                    generated = true;
                }

                addresses = unusedAddresses.Concat(newAddresses).OrderBy(x => x.Index).Take(count);
            }

            if (generated)
            {
                // Save the changes to the file.
                this.SaveWallet(wallet);

                return alwaysnew ? newAddresses : addresses;
            }

            return addresses;
        }

        /// <inheritdoc />
        public (string folderPath, IEnumerable<string>) GetWalletsFiles()
        {
            return (this.fileStorage.FolderPath, this.fileStorage.GetFilesNames(this.GetWalletFileExtension()));
        }

        /// <inheritdoc />
        public IEnumerable<AccountHistory> GetHistory(string walletName, string accountName = null)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            // In order to calculate the fee properly we need to retrieve all the transactions with spending details.
            Types.Wallet wallet = this.GetWalletByName(walletName);

            var accountsHistory = new List<AccountHistory>();

            lock (this.lockObject)
            {
                var accounts = new List<HdAccount>();
                if (!string.IsNullOrEmpty(accountName))
                {
                    HdAccount account = wallet.GetAccount(accountName);
                    if (account == null)
                        throw new WalletException($"No account with the name '{accountName}' could be found.");

                    accounts.Add(account);
                }
                else
                {
                    accounts.AddRange(wallet.GetAccounts());
                }

                foreach (HdAccount account in accounts)
                {
                    accountsHistory.Add(this.GetHistory(wallet, account));
                }
            }

            return accountsHistory;
        }

        /// <inheritdoc />
        public AccountHistory GetHistory(Types.Wallet wallet, HdAccount account)
        {
            Guard.NotNull(account, nameof(account));
            FlatHistory[] items;

            lock (this.lockObject)
            {
                // Get transactions contained in the account.
                var query = account.GetCombinedAddresses().Where(a => wallet.walletStore.CountForAddress(a.Address) > 0);

                if (account.IsNormalAccount())
                {
                    // When the account is a normal one, we want to filter out all cold stake UTXOs.
                    items = query.SelectMany(s => wallet.walletStore.GetForAddress(s.Address)
                                 .Where(t => t.IsColdCoinStake == null || t.IsColdCoinStake == false)
                                 .Select(t => new FlatHistory { Address = s, Transaction = t }))
                                .ToArray();
                }
                else
                {
                    items = query.SelectMany(s => wallet.walletStore.GetForAddress(s.Address)
                                .Select(t => new FlatHistory { Address = s, Transaction = t }))
                                .ToArray();
                }
            }

            return new AccountHistory { Account = account, History = items };
        }

        /// <inheritdoc />
        public IEnumerable<AccountHistorySlim> GetHistorySlim(string walletName, string accountName = null, int skip = 0, int take = 100)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            // In order to calculate the fee properly we need to retrieve all the transactions with spending details.
            Types.Wallet wallet = this.GetWalletByName(walletName);

            var accountsHistory = new List<AccountHistorySlim>();

            lock (this.lockObject)
            {
                var accounts = new List<HdAccount>();
                if (!string.IsNullOrEmpty(accountName))
                {
                    HdAccount account = wallet.GetAccount(accountName);
                    if (account == null)
                        throw new WalletException($"No account with the name '{accountName}' could be found.");

                    accounts.Add(account);
                }
                else
                {
                    accounts.AddRange(wallet.GetAccounts());
                }

                foreach (HdAccount account in accounts)
                {
                    accountsHistory.Add(this.GetHistorySlim(wallet, account, skip, take));
                }
            }

            return accountsHistory;
        }

        /// <inheritdoc />
        public AccountHistorySlim GetHistorySlim(Types.Wallet wallet, HdAccount account, int skip = 0, int take = 100)
        {
            Guard.NotNull(account, nameof(account));
            FlatHistorySlim[] items;

            lock (this.lockObject)
            {
                // Get transactions contained in the account.
                var trxs = wallet.walletStore.GetAccountHistory(account.Index, account.IsNormalAccount(), skip: skip, take: take).ToList();

                items = trxs.Select(s => new FlatHistorySlim { Transaction = s, Address = s.ScriptPubKey != null ? this.walletIndex[wallet.Name].ScriptToAddressLookup[s.ScriptPubKey] : null }).ToArray();
            }

            return new AccountHistorySlim { Account = account, History = items };
        }

        /// <inheritdoc />
        public IEnumerable<AccountBalance> GetBalances(string walletName, string accountName = null, bool calculatSpendable = false)
        {
            var balances = new List<AccountBalance>();

            lock (this.lockObject)
            {
                Types.Wallet wallet = this.GetWalletByName(walletName);

                var accounts = new List<HdAccount>();
                if (!string.IsNullOrEmpty(accountName))
                {
                    HdAccount account = wallet.GetAccount(accountName);
                    if (account == null)
                        throw new WalletException($"No account with the name '{accountName}' could be found.");

                    accounts.Add(account);
                }
                else
                {
                    accounts.AddRange(wallet.GetAccounts());
                }

                foreach (HdAccount account in accounts)
                {
                    Money spendableAmount = Money.Zero;

                    if (calculatSpendable)
                    {
                        // Calculates the amount of spendable coins.
                        UnspentOutputReference[] spendableBalance = account.GetSpendableTransactions(wallet.walletStore, this.ChainIndexer.Tip.Height, this.network.Consensus.CoinbaseMaturity).ToArray();

                        foreach (UnspentOutputReference bal in spendableBalance)
                        {
                            spendableAmount += bal.Transaction.Amount;
                        }
                    }

                    // Get the total balances.
                    WalletBalanceResult result = wallet.walletStore.GetBalanceForAccount(account.Index, account.IsNormalAccount());

                    balances.Add(new AccountBalance
                    {
                        Account = account,
                        AmountConfirmed = result.AmountConfirmed,
                        AmountUnconfirmed = result.AmountUnconfirmed,
                        SpendableAmount = spendableAmount
                    });
                }
            }

            return balances;
        }

        /// <inheritdoc />
        public AddressBalance GetAddressBalance(string address)
        {
            Guard.NotEmpty(address, nameof(address));

            var balance = new AddressBalance
            {
                Address = address,
                CoinType = this.coinType
            };

            lock (this.lockObject)
            {
                HdAddress hdAddress = null;

                foreach (Types.Wallet wallet in this.Wallets)
                {
                    hdAddress = wallet.GetAllAddresses().FirstOrDefault(a => a.Address == address || a.Bech32Address == address);
                    if (hdAddress == null) continue;

                    // When this query to get balance on specific address, we will exclude the cold staking UTXOs.
                    (Money amountConfirmed, Money amountUnconfirmed, bool anyTrx) result = hdAddress.GetBalances(wallet.walletStore, true);

                    Money spendableAmount = wallet
                        .GetAllSpendableTransactions(wallet.walletStore, this.ChainIndexer.Tip.Height)
                        .Where(s => s.Address.Address == hdAddress.Address)
                        .Sum(s => s.Transaction?.Amount ?? 0);

                    balance.AmountConfirmed = result.amountConfirmed;
                    balance.AmountUnconfirmed = result.amountUnconfirmed;
                    balance.SpendableAmount = spendableAmount;

                    break;
                }

                if (hdAddress == null)
                {
                    this.logger.LogTrace("(-)[ADDRESS_NOT_FOUND]");
                    throw new WalletException($"Address '{address}' not found in wallets.");
                }
            }

            return balance;
        }

        /// <inheritdoc />
        public Types.Wallet GetWallet(string walletName)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            Types.Wallet wallet = this.GetWalletByName(walletName);

            return wallet;
        }

        /// <inheritdoc />
        public IEnumerable<HdAccount> GetAccounts(string walletName)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            Types.Wallet wallet = this.GetWalletByName(walletName);

            HdAccount[] res = null;
            lock (this.lockObject)
            {
                res = wallet.GetAccounts().ToArray();
            }
            return res;
        }

        /// <inheritdoc />
        public int LastBlockHeight()
        {
            if (!this.Wallets.Any())
            {
                int height = this.ChainIndexer.Tip.Height;
                this.logger.LogTrace("(-)[NO_WALLET]:{0}", height);
                return height;
            }

            int res;
            lock (this.lockObject)
            {
                res = this.Wallets.Min(w => w.AccountsRoot.Single().LastBlockSyncedHeight) ?? 0;
            }

            return res;
        }

        /// <inheritdoc />
        public bool ContainsWallets => this.Wallets.Any();

        /// <summary>
        /// Gets the hash of the last block received by the wallets.
        /// </summary>
        /// <returns>Hash of the last block received by the wallets.</returns>
        public HashHeightPair LastReceivedBlockInfo()
        {
            if (!this.Wallets.Any())
            {
                ChainedHeader chainedHeader = this.ChainIndexer.Tip;
                this.logger.LogTrace("(-)[NO_WALLET]:'{0}'", chainedHeader);
                return new HashHeightPair(chainedHeader);
            }

            AccountRoot accountRoot;
            lock (this.lockObject)
            {
                accountRoot = this.Wallets
                    .Select(w => w.AccountsRoot.Single())
                    .Where(w => w != null)
                    .OrderBy(o => o.LastBlockSyncedHeight)
                    .FirstOrDefault();

                // If details about the last block synced are not present in the wallet,
                // find out which is the oldest wallet and set the last block synced to be the one at this date.
                if (accountRoot == null || accountRoot.LastBlockSyncedHash == null)
                {
                    this.logger.LogWarning("There were no details about the last block synced in the wallets.");
                    DateTimeOffset earliestWalletDate = this.Wallets.Min(c => c.CreationTime);
                    this.UpdateWhenChainDownloaded(this.Wallets, earliestWalletDate.DateTime);
                    return new HashHeightPair(this.ChainIndexer.Tip);
                }
            }

            return new HashHeightPair(accountRoot.LastBlockSyncedHash, accountRoot.LastBlockSyncedHeight.Value);
        }

        /// <inheritdoc />
        public IEnumerable<UnspentOutputReference> GetSpendableTransactionsInWallet(string walletName, int confirmations = 0)
        {
            return this.GetSpendableTransactionsInWallet(walletName, confirmations, Types.Wallet.NormalAccounts);
        }

        public virtual IEnumerable<UnspentOutputReference> GetSpendableTransactionsInWalletForStaking(string walletName, int confirmations = 0)
        {
            return this.GetUnspentTransactionsInWallet(walletName, confirmations, Types.Wallet.NormalAccounts);
        }

        /// <inheritdoc />
        public IEnumerable<UnspentOutputReference> GetUnspentTransactionsInWallet(string walletName, int confirmations, Func<HdAccount, bool> accountFilter)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            Types.Wallet wallet = this.GetWalletByName(walletName);
            UnspentOutputReference[] res = null;
            lock (this.lockObject)
            {
                res = wallet.GetAllUnspentTransactions(wallet.walletStore, this.ChainIndexer.Tip.Height, confirmations, accountFilter).ToArray();
            }

            return res;
        }

        public IEnumerable<UnspentOutputReference> GetSpendableTransactionsInWallet(string walletName, int confirmations, Func<HdAccount, bool> accountFilter)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            Types.Wallet wallet = this.GetWalletByName(walletName);
            UnspentOutputReference[] res = null;
            lock (this.lockObject)
            {
                res = wallet.GetAllSpendableTransactions(wallet.walletStore, this.ChainIndexer.Tip.Height, confirmations, accountFilter).ToArray();
            }

            return res;
        }

        /// <inheritdoc />
        public IEnumerable<UnspentOutputReference> GetSpendableTransactionsInAccount(WalletAccountReference walletAccountReference, int confirmations = 0)
        {
            Guard.NotNull(walletAccountReference, nameof(walletAccountReference));

            Types.Wallet wallet = this.GetWalletByName(walletAccountReference.WalletName);
            UnspentOutputReference[] res = null;
            lock (this.lockObject)
            {
                HdAccount account = wallet.GetAccount(walletAccountReference.AccountName);

                if (account == null)
                {
                    this.logger.LogTrace("(-)[ACT_NOT_FOUND]");
                    throw new WalletException(
                        $"Account '{walletAccountReference.AccountName}' in wallet '{walletAccountReference.WalletName}' not found.");
                }

                res = account.GetSpendableTransactions(wallet.walletStore, this.ChainIndexer.Tip.Height, this.network.Consensus.CoinbaseMaturity, confirmations).ToArray();
            }

            return res;
        }

        /// <inheritdoc />
        public void RemoveBlocks(ChainedHeader fork)
        {
            Guard.NotNull(fork, nameof(fork));

            lock (this.lockObject)
            {
                foreach (WalletIndex walletIndex in this.walletIndex.Values)
                {
                    foreach (HdAddress address in walletIndex.ScriptToAddressLookup.Values)
                    {
                        // Remove all the UTXO that have been reorged.
                        var allStransactions = walletIndex.Wallet.walletStore.GetForAddress(address.Address).ToList();
                        IEnumerable<TransactionOutputData> makeUnspendable = allStransactions.Where(w => w.BlockHeight > fork.Height).ToList();
                        foreach (TransactionOutputData transactionData in makeUnspendable)
                        {
                            walletIndex.Wallet.walletStore.Remove(transactionData.OutPoint);
                        }

                        // Bring back all the UTXO that are now spendable after the reorg.
                        IEnumerable<TransactionOutputData> makeSpendable = allStransactions.Where(w => (w.SpendingDetails != null) && (w.SpendingDetails.BlockHeight > fork.Height));
                        foreach (TransactionOutputData transactionData in makeSpendable)
                        {
                            transactionData.SpendingDetails = null;
                            walletIndex.Wallet.walletStore.InsertOrUpdate(transactionData);
                        }
                    }
                }
                this.UpdateLastBlockSyncedHeight(fork);

                // Reload the lookup dictionaries.
                this.RefreshInputKeysLookup();
            }
        }

        /// <inheritdoc />
        public void ProcessBlock(Block block, ChainedHeader chainedHeader)
        {
            Guard.NotNull(block, nameof(block));
            Guard.NotNull(chainedHeader, nameof(chainedHeader));

            // If there is no wallet yet, update the wallet tip hash and do nothing else.
            if (!this.Wallets.Any())
            {
                this.WalletTipHash = chainedHeader.HashBlock;
                this.WalletTipHeight = chainedHeader.Height;
                this.logger.LogTrace("(-)[NO_WALLET]");
                return;
            }

            // Is this the next block.
            if (chainedHeader.Header.HashPrevBlock != this.WalletTipHash)
            {
                this.logger.LogDebug("New block's previous hash '{0}' does not match current wallet's tip hash '{1}'.", chainedHeader.Header.HashPrevBlock, this.WalletTipHash);

                // The block coming in to the wallet should never be ahead of the wallet.
                // If the block is behind, let it pass.
                if (chainedHeader.Height > this.WalletTipHeight)
                {
                    this.logger.LogTrace("(-)[BLOCK_TOO_FAR]");
                    throw new WalletException("block too far in the future has arrived to the wallet");
                }
            }

            lock (this.lockObject)
            {
                bool trxFoundInBlock = false;
                foreach (Transaction transaction in block.Transactions)
                {
                    bool trxFound = this.ProcessTransaction(transaction, chainedHeader.Height, block, true);
                    if (trxFound)
                    {
                        trxFoundInBlock = true;
                    }
                }

                // Update the wallets with the last processed block height.
                // It's important that updating the height happens after the block processing is complete,
                // as if the node is stopped, on re-opening it will start updating from the previous height.
                this.UpdateLastBlockSyncedHeight(chainedHeader);

                if (trxFoundInBlock)
                {
                    this.logger.LogDebug("Block {0} contains at least one transaction affecting the user's wallet(s).", chainedHeader);
                }
            }
        }

        /// <inheritdoc />
        public virtual bool ProcessTransaction(Transaction transaction, int? blockHeight = null, Block block = null, bool isPropagated = true)
        {
            Guard.NotNull(transaction, nameof(transaction));
            uint256 hash = transaction.GetHash();

            bool foundReceivingTrx = false, foundSendingTrx = false;

            lock (this.lockObject)
            {
                if (block != null)
                {
                    // Do a pre-scan of the incoming transaction's inputs to see if they're used in other wallet transactions already.
                    foreach (TxIn input in transaction.Inputs)
                    {
                        foreach (KeyValuePair<string, WalletIndex> walletIndexItem in this.walletIndex)
                        {
                            // See if this input is being used by another wallet transaction present in the index.
                            // The inputs themselves may not belong to the wallet, but the transaction data in the index has to be for a wallet transaction.
                            if (walletIndexItem.Value.InputLookup.TryGetValue(input.PrevOut, out OutPoint outPoint))
                            {
                                TransactionOutputData indexData = walletIndexItem.Value.Wallet.walletStore.GetForOutput(outPoint);

                                // It's the same transaction, which can occur if the transaction had been added to the wallet previously. Ignore.
                                if (indexData.Id == hash)
                                    continue;

                                if (indexData.BlockHash != null)
                                {
                                    // This should not happen as pre checks are done in mempool and consensus.
                                    throw new WalletException("The same inputs were found in two different confirmed transactions");
                                }

                                // This is a double spend we remove the unconfirmed trx
                                this.RemoveTransactionsByIds(new[] { indexData.Id });
                                walletIndexItem.Value.InputLookup.Remove(input.PrevOut);
                            }
                        }
                    }
                }

                // Check the outputs, ignoring the ones with a 0 amount.
                foreach (TxOut utxo in transaction.Outputs.Where(o => o.Value != Money.Zero))
                {
                    foreach (KeyValuePair<string, WalletIndex> walletIndexItem in this.walletIndex)
                    {
                        // Check if the outputs contain one of our addresses.
                        if (walletIndexItem.Value.ScriptToAddressLookup.TryGetValue(utxo.ScriptPubKey, out HdAddress address))
                        {
                            Types.Wallet wallet = this.Wallets.First(f => f.Name == walletIndexItem.Key);

                            this.AddTransactionToWallet(wallet, address, transaction, utxo, blockHeight, block, isPropagated);
                            foundReceivingTrx = true;
                            this.logger.LogDebug("Transaction '{0}' contained funds received by the user's wallet(s).", hash);

                            this.signals?.Publish(new Events.TransactionFound(transaction));
                        }
                    }
                }

                // Check the inputs - include those that have a reference to a transaction containing one of our scripts and the same index.
                foreach (TxIn input in transaction.Inputs)
                {
                    foreach (KeyValuePair<string, WalletIndex> walletIndexItem in this.walletIndex)
                    {
                        if (!walletIndexItem.Value.OutputLookup.TryGetValue(input.PrevOut, out string _))
                        {
                            continue;
                        }

                        this.AddSpendingTransactionToWallet(walletIndexItem.Value.Wallet, transaction, input.PrevOut, walletIndexItem.Value.Wallet, blockHeight, block);
                        foundSendingTrx = true;
                        this.logger.LogDebug("Transaction '{0}' contained funds sent by the user's wallet(s).", hash);

                        this.signals?.Publish(new Events.TransactionSpent(transaction, input.PrevOut));
                    }
                }
            }

            return foundSendingTrx || foundReceivingTrx;
        }

        /// <summary>
        /// Adds a transaction that credits the wallet with new coins.
        /// This method is can be called many times for the same transaction (idempotent).
        /// </summary>
        /// <param name="wallet">The wallet of the address.</param>
        /// <param name="address">The address of the script found for the transaction.</param>
        /// <param name="transaction">The transaction from which details are added.</param>
        /// <param name="utxo">The unspent output to add to the wallet.</param>
        /// <param name="blockHeight">Height of the block.</param>
        /// <param name="block">The block containing the transaction to add.</param>
        /// <param name="isPropagated">Propagation state of the transaction.</param>
        private void AddTransactionToWallet(Types.Wallet wallet, HdAddress address, Transaction transaction, TxOut utxo, int? blockHeight = null, Block block = null, bool isPropagated = true)
        {
            Guard.NotNull(transaction, nameof(transaction));
            Guard.NotNull(utxo, nameof(utxo));

            uint256 transactionHash = transaction.GetHash();

            // Get the ColdStaking script template if available.
            Dictionary<string, ScriptTemplate> templates = this.GetValidStakingTemplates();
            ScriptTemplate coldStakingTemplate = templates.ContainsKey("ColdStaking") ? templates["ColdStaking"] : null;

            // Get the collection of transactions to add to.
            Script script = utxo.ScriptPubKey;

            // Check if a similar UTXO exists or not (same transaction ID and same index).
            // New UTXOs are added, existing ones are updated.
            int index = transaction.Outputs.IndexOf(utxo);
            Money amount = utxo.Value;
            var outPoint = new OutPoint(transactionHash, index);
            TransactionOutputData foundTransaction = wallet.walletStore.GetForOutput(outPoint);
            if (foundTransaction == null)
            {
                this.logger.LogDebug("UTXO '{0}-{1}' not found, creating.", transactionHash, index);
                var newTransaction = new TransactionOutputData
                {
                    OutPoint = outPoint,
                    Address = address.Address,
                    AccountIndex = HdOperations.GetAccountIndex(address.HdPath),
                    Amount = amount,
                    IsCoinBase = transaction.IsCoinBase == false ? (bool?)null : true,
                    IsCoinStake = transaction.IsCoinStake == false ? (bool?)null : true,
                    IsColdCoinStake = (coldStakingTemplate != null && coldStakingTemplate.CheckScriptPubKey(script)) == false ? (bool?)null : true,
                    BlockHeight = blockHeight,
                    BlockHash = block?.GetHash(),
                    BlockIndex = block?.Transactions.FindIndex(t => t.GetHash() == transactionHash),
                    Id = transactionHash,
                    CreationTime = DateTimeOffset.FromUnixTimeSeconds(block?.Header.Time ?? this.dateTimeProvider.GetTime()),
                    Index = index,
                    ScriptPubKey = script,
                    Hex = this.walletSettings.SaveTransactionHex ? transaction.ToHex() : null,
                    IsPropagated = isPropagated,
                };

                // Add the Merkle proof to the (non-spending) transaction.
                if (block != null)
                {
                    newTransaction.MerkleProof = new MerkleBlock(block, new[] { transactionHash }).PartialMerkleTree;
                }

                wallet.walletStore.InsertOrUpdate(newTransaction);
                this.AddInputKeysLookup(wallet, newTransaction.OutPoint);

                if (block == null)
                {
                    // Unconfirmed inputs track for double spends.
                    this.AddTxLookup(wallet, newTransaction, transaction);
                }
            }
            else
            {
                this.logger.LogDebug("Transaction ID '{0}' found, updating.", transactionHash);

                bool changed = false;

                // Update the block height and block hash.
                if ((foundTransaction.BlockHeight == null) && (blockHeight != null))
                {
                    foundTransaction.BlockHeight = blockHeight;
                    foundTransaction.BlockHash = block?.GetHash();
                    foundTransaction.BlockIndex = block?.Transactions.FindIndex(t => t.GetHash() == transactionHash);
                    changed = true;
                }

                // Update the block time.
                if (block != null)
                {
                    foundTransaction.CreationTime = DateTimeOffset.FromUnixTimeSeconds(block.Header.Time);
                    changed = true;
                }

                // Add the Merkle proof now that the transaction is confirmed in a block.
                if ((block != null) && (foundTransaction.MerkleProof == null))
                {
                    foundTransaction.MerkleProof = new MerkleBlock(block, new[] { transactionHash }).PartialMerkleTree;
                    changed = true;
                }

                if (isPropagated)
                {
                    foundTransaction.IsPropagated = true;
                    changed = true;
                }

                if (changed)
                {
                    wallet.walletStore.InsertOrUpdate(foundTransaction);
                }

                if (block != null)
                {
                    // Inputs are in a block no need to track them anymore.
                    this.RemoveTxLookup(wallet, transaction);
                }
            }

            this.TransactionFoundInternal(wallet, script);
        }

        /// <summary>
        /// Mark an output as spent, the credit of the output will not be used to calculate the balance.
        /// The output will remain in the wallet for history (and reorg).
        /// </summary>
        /// <param name="transaction">The transaction from which details are added.</param>
        /// <param name="spentTransaction">The trx output.</param>
        /// <param name="spentTransactionWallet">The wallet associated with this transaction.</param>
        /// <param name="blockHeight">Height of the block.</param>
        /// <param name="block">The block containing the transaction to add.</param>
        private void AddSpendingTransactionToWallet(Types.Wallet wallet, Transaction transaction, OutPoint outPoint,
            Types.Wallet spentTransactionWallet, int? blockHeight = null, Block block = null)
        {
            Guard.NotNull(transaction, nameof(transaction));

            uint256 transactionHash = transaction.GetHash();
            TransactionOutputData spentTransaction = wallet.walletStore.GetForOutput(outPoint);
            this.walletIndex[spentTransactionWallet.Name].ScriptToAddressLookup.TryGetValue(spentTransaction.ScriptPubKey, out HdAddress spentDestination);

            // If the details of this spending transaction are seen for the first time.
            if (spentTransaction.SpendingDetails == null)
            {
                this.logger.LogDebug("Spending UTXO '{0}-{1}' is new.", spentTransaction.Id, spentTransaction.Index);

                var payments = new List<PaymentDetails>();

                // Get the details of the outputs paid out.
                foreach (TxOut paidToOutput in transaction.Outputs)
                {
                    // If script is empty ignore it.
                    if (paidToOutput.IsEmpty)
                        continue;

                    if (StandardTransactionPolicy.IsOpReturn(paidToOutput.ScriptPubKey.ToBytes()))
                        continue;

                    // Check if the destination script is one of the wallet's.
                    bool isPaytoSelf = false;
                    if (this.walletIndex[spentTransactionWallet.Name].ScriptToAddressLookup.TryGetValue(paidToOutput.ScriptPubKey, out HdAddress destination))
                    {
                        if ((spentDestination != null) && (HdOperations.GetAccountIndex(destination.HdPath) != HdOperations.GetAccountIndex(spentDestination.HdPath)))
                        {
                            // payments between the different accounts are not to self payments
                            isPaytoSelf = false;
                        }
                        else
                        {
                            isPaytoSelf = true;
                        }
                    }

                    // Figure out how to retrieve the destination address.
                    string destinationAddress = this.scriptAddressReader.GetAddressFromScriptPubKey(this.network, paidToOutput.ScriptPubKey);
                    if (string.IsNullOrEmpty(destinationAddress))
                        if (destination != null)
                            destinationAddress = destination.Address;

                    payments.Add(new PaymentDetails
                    {
                        DestinationScriptPubKey = paidToOutput.ScriptPubKey,
                        DestinationAddress = destinationAddress,
                        Amount = paidToOutput.Value,
                        OutputIndex = transaction.Outputs.IndexOf(paidToOutput),
                        PayToSelf = isPaytoSelf,
                    });
                }

                var spendingDetails = new SpendingDetails
                {
                    TransactionId = transactionHash,
                    Payments = payments,
                    CreationTime = DateTimeOffset.FromUnixTimeSeconds(block?.Header.Time ?? this.dateTimeProvider.GetTime()),
                    BlockHeight = blockHeight,
                    BlockIndex = block?.Transactions.FindIndex(t => t.GetHash() == transactionHash),
                    Hex = this.walletSettings.SaveTransactionHex ? transaction.ToHex() : null,
                    IsCoinStake = transaction.IsCoinStake == false ? (bool?)null : true
                };

                spentTransaction.SpendingDetails = spendingDetails;
                spentTransaction.MerkleProof = null;

                // Push the changes to disk
                wallet.walletStore.InsertOrUpdate(spentTransaction);
            }
            else // If this spending transaction is being confirmed in a block.
            {
                this.logger.LogDebug("Spending transaction ID '{0}' is being confirmed, updating.", spentTransaction.Id);

                bool changed = false;

                // Update the block height.
                if (spentTransaction.SpendingDetails.BlockHeight == null && blockHeight != null)
                {
                    spentTransaction.SpendingDetails.BlockHeight = blockHeight;
                    changed = true;
                }

                // Update the block time to be that of the block in which the transaction is confirmed.
                if (block != null)
                {
                    spentTransaction.SpendingDetails.CreationTime = DateTimeOffset.FromUnixTimeSeconds(block.Header.Time);
                    spentTransaction.BlockIndex = block?.Transactions.FindIndex(t => t.GetHash() == transactionHash);
                    changed = true;
                }

                if (changed)
                {
                    // Push the changes to disk
                    wallet.walletStore.InsertOrUpdate(spentTransaction);
                }
            }

            // If the transaction is spent and confirmed, we remove the UTXO from the lookup dictionary.
            if (spentTransaction.SpendingDetails.BlockHeight != null)
            {
                this.RemoveInputKeysLookup(spentTransactionWallet, spentTransaction);
            }
        }

        public virtual void TransactionFoundInternal(Types.Wallet wallet, Script script, Func<HdAccount, bool> accountFilter = null)
        {
            // An address has no knowledge whether its 'change' or not
            // so we can't use the indexer, iterate over each account
            // addresses collection to find if its change or not
            // (this can be optimized by addinga flag to the HdAddress class).

            foreach (HdAccount account in wallet.GetAccounts(accountFilter ?? Types.Wallet.NormalAccounts))
            {
                bool isChange;
                if (account.ExternalAddresses.Any(address => address.ScriptPubKey == script))
                {
                    isChange = false;
                }
                else if (account.InternalAddresses.Any(address => address.ScriptPubKey == script))
                {
                    isChange = true;
                }
                else
                {
                    continue;
                }

                IEnumerable<HdAddress> newAddresses = this.AddAddressesToMaintainBuffer(wallet, account, isChange);

                this.UpdateKeysLookup(wallet, newAddresses);
            }
        }

        private IEnumerable<HdAddress> AddAddressesToMaintainBuffer(Types.Wallet wallet, HdAccount account, bool isChange)
        {
            HdAddress lastUsedAddress = account.GetLastUsedAddress(wallet.walletStore, isChange);
            int lastUsedAddressIndex = lastUsedAddress?.Index ?? -1;
            int addressesCount = isChange ? account.InternalAddresses.Count() : account.ExternalAddresses.Count();
            int emptyAddressesCount = addressesCount - lastUsedAddressIndex - 1;
            int addressesToAdd = this.walletSettings.UnusedAddressesBuffer - emptyAddressesCount;

            return addressesToAdd > 0 ? account.CreateAddresses(this.network, addressesToAdd, isChange) : new List<HdAddress>();
        }

        /// <inheritdoc />
        public void DeleteWallet()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SaveWallets()
        {
            foreach (Types.Wallet wallet in this.Wallets)
            {
                this.SaveWallet(wallet);
            }
        }

        /// <inheritdoc />
        public void SaveWallet(Types.Wallet wallet)
        {
            Guard.NotNull(wallet, nameof(wallet));

            lock (this.lockObject)
            {
                WalletData walletData = wallet.walletStore.GetData();
                walletData.BlockLocator = wallet.BlockLocator;
                AccountRoot accountRoot = wallet.AccountsRoot.Single();
                walletData.WalletTip = new HashHeightPair(accountRoot.LastBlockSyncedHash, accountRoot.LastBlockSyncedHeight.Value);

                wallet.walletStore.SetData(walletData);

                this.fileStorage.SaveToFile(wallet, $"{wallet.Name}.{WalletFileExtension}", new FileStorageOption { SerializeNullValues = false });
            }
        }

        /// <inheritdoc />
        public string GetWalletFileExtension()
        {
            return WalletFileExtension;
        }

        /// <inheritdoc />
        public void UpdateLastBlockSyncedHeight(ChainedHeader chainedHeader)
        {
            Guard.NotNull(chainedHeader, nameof(chainedHeader));

            // Update the wallets with the last processed block height.
            foreach (Types.Wallet wallet in this.Wallets)
            {
                this.UpdateLastBlockSyncedHeight(wallet, chainedHeader);
            }

            this.WalletTipHash = chainedHeader.HashBlock;
            this.WalletTipHeight = chainedHeader.Height;
        }

        /// <inheritdoc />
        public void UpdateLastBlockSyncedHeight(Types.Wallet wallet, ChainedHeader chainedHeader)
        {
            Guard.NotNull(wallet, nameof(wallet));
            Guard.NotNull(chainedHeader, nameof(chainedHeader));

            // The block locator will help when the wallet
            // needs to rewind this will be used to find the fork.
            wallet.BlockLocator = chainedHeader.GetLocator().Blocks;

            lock (this.lockObject)
            {
                wallet.SetLastBlockDetails(chainedHeader);
            }
        }

        /// <summary>
        /// Generates the wallet file.
        /// </summary>
        /// <param name="name">The name of the wallet.</param>
        /// <param name="encryptedSeed">The seed for this wallet, password encrypted.</param>
        /// <param name="chainCode">The chain code.</param>
        /// <param name="creationTime">The time this wallet was created.</param>
        /// <param name="coinType">A BIP44 coin type, this will allow to overwrite the default network coin type.</param>
        /// <returns>The wallet object that was saved into the file system.</returns>
        /// <exception cref="WalletException">Thrown if wallet cannot be created.</exception>
        private Types.Wallet GenerateWalletFile(string name, string encryptedSeed, byte[] chainCode, DateTimeOffset? creationTime = null, int? coinType = null)
        {
            Guard.NotEmpty(name, nameof(name));
            Guard.NotEmpty(encryptedSeed, nameof(encryptedSeed));
            Guard.NotNull(chainCode, nameof(chainCode));

            // Check if any wallet file already exists, with case insensitive comparison.
            if (this.Wallets.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                this.logger.LogTrace("(-)[WALLET_ALREADY_EXISTS]");
                throw new WalletException($"Wallet with name '{name}' already exists.");
            }

            List<Types.Wallet> similarWallets = this.Wallets.Where(w => w.EncryptedSeed == encryptedSeed).ToList();
            if (similarWallets.Any())
            {
                this.logger.LogTrace("(-)[SAME_PK_ALREADY_EXISTS]");
                throw new WalletException("Cannot create this wallet as a wallet with the same private key already exists. If you want to restore your wallet from scratch, " +
                                                    $"please remove the file {string.Join(", ", similarWallets.Select(w => w.Name))}.{WalletFileExtension} from '{this.fileStorage.FolderPath}' and try restoring the wallet again. " +
                                                    "Make sure you have your mnemonic and your password handy!");
            }

            var walletFile = new Types.Wallet
            {
                Name = name,
                EncryptedSeed = encryptedSeed,
                ChainCode = chainCode,
                CreationTime = creationTime ?? this.dateTimeProvider.GetTimeOffset(),
                Network = this.network,
                AccountsRoot = new List<AccountRoot> { new AccountRoot() { Accounts = new List<HdAccount>(), CoinType = coinType ?? this.coinType, LastBlockSyncedHeight = 0, LastBlockSyncedHash = this.network.GenesisHash } },
            };

            walletFile.walletStore = new WalletStore(this.network, this.dataFolder, walletFile);

            // Create a folder if none exists and persist the file.
            this.SaveWallet(walletFile);

            return walletFile;
        }

        /// <summary>
        /// Generates the wallet file without private key and chain code.
        /// For use with only the extended public key.
        /// </summary>
        /// <param name="name">The name of the wallet.</param>
        /// <param name="creationTime">The time this wallet was created.</param>
        /// <returns>The wallet object that was saved into the file system.</returns>
        /// <exception cref="WalletException">Thrown if wallet cannot be created.</exception>
        private Types.Wallet GenerateExtPubKeyOnlyWalletFile(string name, DateTimeOffset? creationTime = null)
        {
            Guard.NotEmpty(name, nameof(name));

            // Check if any wallet file already exists, with case insensitive comparison.
            if (this.Wallets.Any(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                this.logger.LogTrace("(-)[WALLET_ALREADY_EXISTS]");
                throw new WalletException($"Wallet with name '{name}' already exists.");
            }

            var walletFile = new Types.Wallet
            {
                Name = name,
                IsExtPubKeyWallet = true,
                CreationTime = creationTime ?? this.dateTimeProvider.GetTimeOffset(),
                Network = this.network,
                AccountsRoot = new List<AccountRoot> { new AccountRoot() { Accounts = new List<HdAccount>(), CoinType = this.coinType, LastBlockSyncedHeight = 0, LastBlockSyncedHash = this.network.GenesisHash } },
            };

            walletFile.walletStore = new WalletStore(this.network, this.dataFolder, walletFile);

            // Create a folder if none exists and persist the file.
            this.SaveWallet(walletFile);

            return walletFile;
        }

        /// <summary>
        /// Loads the wallet to be used by the manager if a wallet with this name has not already been loaded.
        /// </summary>
        /// <param name="wallet">The wallet to load.</param>
        internal void Load(Types.Wallet wallet)
        {
            Guard.NotNull(wallet, nameof(wallet));

            if (this.Wallets.Any(w => w.Name == wallet.Name))
            {
                this.logger.LogTrace("(-)[NOT_FOUND]");
                return;
            }

            if (wallet.walletStore == null)
            {
                wallet.walletStore = new WalletStore(this.network, this.dataFolder, wallet);
            }

            wallet.BlockLocator = wallet.walletStore.GetData().BlockLocator;
            wallet.AccountsRoot.Single().LastBlockSyncedHash = wallet.walletStore.GetData().WalletTip.Hash;
            wallet.AccountsRoot.Single().LastBlockSyncedHeight = wallet.walletStore.GetData().WalletTip.Height;

            this.Wallets.Add(wallet);
        }

        /// <summary>
        /// Loads the keys and transactions we're tracking in memory for faster lookups.
        /// </summary>
        public void LoadKeysLookup()
        {
            lock (this.lockObject)
            {
                foreach (Types.Wallet wallet in this.Wallets)
                {
                    foreach (HdAccount account in wallet.GetAccounts(a => true))
                    {
                        foreach (HdAddress address in account.GetCombinedAddresses())
                        {
                            this.AddAddressToIndex(wallet, address);

                            foreach (TransactionOutputData transaction in wallet.walletStore.GetForAddress(address.Address))
                            {
                                // Get the UTXOs that are unspent or spent but not confirmed.
                                // We only exclude from the list the confirmed spent UTXOs.
                                if (transaction.SpendingDetails?.BlockHeight == null)
                                {
                                    this.walletIndex[wallet.Name].OutputLookup[transaction.OutPoint] = null;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void AddAddressToIndex(Types.Wallet wallet, HdAddress address)
        {
            WalletIndex walletIndex = null;

            if (!this.walletIndex.ContainsKey(wallet.Name))
            {
                walletIndex = new WalletIndex()
                {
                    InputLookup = new Dictionary<OutPoint, OutPoint>(),
                    OutputLookup = new Dictionary<OutPoint, string>(),
                    ScriptToAddressLookup = this.CreateAddressFromScriptLookup(),
                    Wallet = wallet
                };

                this.walletIndex.Add(wallet.Name, walletIndex);
            }
            else
            {
                walletIndex = this.walletIndex[wallet.Name];
            }

            // Track the P2PKH of this pubic key
            walletIndex.ScriptToAddressLookup[address.ScriptPubKey] = address;

            // Track the P2PK of this public key
            if (address.Pubkey != null)
                walletIndex.ScriptToAddressLookup[address.Pubkey] = address;

            // Track the P2WPKH of this pubic key
            if (address.Bech32Address != null)
                walletIndex.ScriptToAddressLookup[new BitcoinWitPubKeyAddress(address.Bech32Address, this.network).ScriptPubKey] = address;
        }

        /// <summary>
        /// Update the keys and transactions we're tracking in memory for faster lookups.
        /// </summary>
        public void UpdateKeysLookup(Types.Wallet wallet, IEnumerable<HdAddress> addresses)
        {
            if (addresses == null || !addresses.Any())
            {
                return;
            }

            lock (this.lockObject)
            {
                foreach (HdAddress address in addresses)
                {
                    this.AddAddressToIndex(wallet, address);
                }
            }
        }

        /// <summary>
        /// Add to the list of unspent outputs kept in memory for faster lookups.
        /// </summary>
        private void AddInputKeysLookup(Types.Wallet wallet, OutPoint outPoint)
        {
            Guard.NotNull(outPoint, nameof(outPoint));

            lock (this.lockObject)
            {
                this.walletIndex[wallet.Name].OutputLookup[outPoint] = null;
            }
        }

        /// <summary>
        /// Remove from the list of unspent outputs kept in memory.
        /// </summary>
        private void RemoveInputKeysLookup(Types.Wallet wallet, TransactionOutputData transactionData)
        {
            Guard.NotNull(transactionData, nameof(transactionData));
            Guard.NotNull(transactionData.SpendingDetails, nameof(transactionData.SpendingDetails));

            lock (this.lockObject)
            {
                this.walletIndex[wallet.Name].OutputLookup.Remove(transactionData.OutPoint);
            }
        }

        /// <summary>
        /// Reloads the UTXOs we're tracking in memory for faster lookups.
        /// </summary>
        public void RefreshInputKeysLookup()
        {
            lock (this.lockObject)
            {
                foreach (Types.Wallet wallet in this.Wallets)
                {
                    this.walletIndex[wallet.Name].OutputLookup = new Dictionary<OutPoint, string>();

                    foreach (HdAddress address in wallet.GetAllAddresses(a => true))
                    {
                        // Get the UTXOs that are unspent or spent but not confirmed.
                        // We only exclude from the list the confirmed spent UTXOs.
                        foreach (TransactionOutputData transaction in wallet.walletStore.GetForAddress(address.Address).Where(t => t.SpendingDetails?.BlockHeight == null))
                        {
                            this.walletIndex[wallet.Name].OutputLookup[transaction.OutPoint] = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add to the mapping of transactions kept in memory for faster lookups.
        /// </summary>
        private void AddTxLookup(Types.Wallet wallet, TransactionOutputData transactionData, Transaction transaction)
        {
            Guard.NotNull(transaction, nameof(transaction));
            Guard.NotNull(transactionData, nameof(transactionData));

            lock (this.lockObject)
            {
                foreach (OutPoint input in transaction.Inputs.Select(s => s.PrevOut))
                {
                    this.walletIndex[wallet.Name].InputLookup[input] = transactionData.OutPoint;
                }
            }
        }

        private void RemoveTxLookup(Types.Wallet wallet, Transaction transaction)
        {
            Guard.NotNull(transaction, nameof(transaction));

            lock (this.lockObject)
            {
                foreach (OutPoint input in transaction.Inputs.Select(s => s.PrevOut))
                {
                    this.walletIndex[wallet.Name].InputLookup.Remove(input);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetWalletsNames()
        {
            return this.Wallets.Select(w => w.Name);
        }

        /// <inheritdoc />
        public Types.Wallet GetWalletByName(string walletName)
        {
            Types.Wallet wallet = this.Wallets.SingleOrDefault(w => w.Name == walletName);
            if (wallet == null)
            {
                this.logger.LogTrace("(-)[WALLET_NOT_FOUND]");
                throw new WalletException($"No wallet with name '{walletName}' could be found.");
            }

            return wallet;
        }

        /// <inheritdoc />
        public ICollection<uint256> GetFirstWalletBlockLocator()
        {
            return this.Wallets.First().BlockLocator;
        }

        /// <inheritdoc />
        public int? GetEarliestWalletHeight()
        {
            return this.Wallets.Min(w => w.AccountsRoot.Single().LastBlockSyncedHeight);
        }

        /// <inheritdoc />
        public DateTimeOffset GetOldestWalletCreationTime()
        {
            return this.Wallets.Min(w => w.CreationTime);
        }

        /// <summary>
        /// Search all wallets and removes the specified transactions from the wallet and persist it.
        /// </summary>
        private void RemoveTransactionsByIds(IEnumerable<uint256> transactionsIds)
        {
            Guard.NotNull(transactionsIds, nameof(transactionsIds));

            foreach (Types.Wallet wallet in this.Wallets)
            {
                this.RemoveTransactionsByIds(wallet.Name, transactionsIds);
            }
        }

        /// <inheritdoc />
        public HashSet<(uint256, DateTimeOffset)> RemoveTransactionsByIds(string walletName, IEnumerable<uint256> transactionsIds)
        {
            Guard.NotNull(transactionsIds, nameof(transactionsIds));
            Guard.NotEmpty(walletName, nameof(walletName));

            List<uint256> idsToRemove = transactionsIds.ToList();
            Types.Wallet wallet = this.GetWallet(walletName);

            var result = new HashSet<(uint256, DateTimeOffset)>();

            lock (this.lockObject)
            {
                IEnumerable<HdAccount> accounts = wallet.GetAccounts(a => true);
                foreach (HdAccount account in accounts)
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        IEnumerable<TransactionOutputData> transactions = wallet.walletStore.GetForAddress(address.Address);

                        foreach (TransactionOutputData transaction in transactions)
                        {
                            // Remove the transaction from the list of transactions affecting an address.
                            // Only transactions that haven't been confirmed in a block can be removed.
                            if (!transaction.IsConfirmed() && idsToRemove.Contains(transaction.Id))
                            {
                                result.Add((transaction.Id, transaction.CreationTime));
                                wallet.walletStore.Remove(transaction.OutPoint);
                            }

                            // Remove the spending transaction object containing this transaction id.
                            if (transaction.SpendingDetails != null && !transaction.SpendingDetails.IsSpentConfirmed() && idsToRemove.Contains(transaction.SpendingDetails.TransactionId))
                            {
                                result.Add((transaction.SpendingDetails.TransactionId, transaction.SpendingDetails.CreationTime));
                                transaction.SpendingDetails = null;
                                wallet.walletStore.InsertOrUpdate(transaction);
                            }
                        }
                    }
                }
            }

            if (result.Any())
            {
                // Reload the lookup dictionaries.
                this.RefreshInputKeysLookup();

                this.SaveWallet(wallet);
            }

            return result;
        }

        /// <inheritdoc />
        public HashSet<(uint256, DateTimeOffset)> RemoveAllTransactions(string walletName)
        {
            Guard.NotEmpty(walletName, nameof(walletName));
            Types.Wallet wallet = this.GetWallet(walletName);

            var removedTransactions = new HashSet<(uint256, DateTimeOffset)>();

            lock (this.lockObject)
            {
                IEnumerable<HdAccount> accounts = wallet.GetAccounts(Types.Wallet.AllAccounts);
                foreach (HdAccount account in accounts)
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        foreach (TransactionOutputData transaction in wallet.walletStore.GetForAddress(address.Address))
                        {
                            removedTransactions.Add((transaction.Id, transaction.CreationTime));
                            wallet.walletStore.Remove(transaction.OutPoint);
                        }
                    }
                }

                // Reload the lookup dictionaries.
                this.RefreshInputKeysLookup();
            }

            if (removedTransactions.Any())
            {
                this.SaveWallet(wallet);
            }

            return removedTransactions;
        }

        /// <inheritdoc />
        public HashSet<(uint256, DateTimeOffset)> RemoveTransactionsFromDate(string walletName, DateTimeOffset fromDate)
        {
            Guard.NotEmpty(walletName, nameof(walletName));
            Types.Wallet wallet = this.GetWallet(walletName);

            var removedTransactions = new HashSet<(uint256, DateTimeOffset)>();

            lock (this.lockObject)
            {
                IEnumerable<HdAccount> accounts = wallet.GetAccounts();
                foreach (HdAccount account in accounts)
                {
                    foreach (HdAddress address in account.GetCombinedAddresses())
                    {
                        var toRemove = wallet.walletStore.GetForAddress(address.Address).Where(t => t.CreationTime > fromDate).ToList();
                        foreach (var trx in toRemove)
                        {
                            removedTransactions.Add((trx.Id, trx.CreationTime));
                            wallet.walletStore.Remove(trx.OutPoint);
                        }
                    }
                }

                // Reload the lookup dictionaries.
                this.RefreshInputKeysLookup();
            }

            if (removedTransactions.Any())
            {
                this.SaveWallet(wallet);
            }

            return removedTransactions;
        }

        /// <summary>
        /// Updates details of the last block synced in a wallet when the chain of headers finishes downloading.
        /// </summary>
        /// <param name="wallets">The wallets to update when the chain has downloaded.</param>
        /// <param name="date">The creation date of the block with which to update the wallet.</param>
        private void UpdateWhenChainDownloaded(IEnumerable<Types.Wallet> wallets, DateTime date)
        {
            if (this.asyncProvider.IsAsyncLoopRunning(DownloadChainLoop))
            {
                return;
            }

            this.asyncProvider.CreateAndRunAsyncLoopUntil(DownloadChainLoop, this.nodeLifetime.ApplicationStopping,
                () => this.ChainIndexer.IsDownloaded(),
                () =>
                {
                    int heightAtDate = this.ChainIndexer.GetHeightAtTime(date);

                    foreach (Types.Wallet wallet in wallets)
                    {
                        var acc = wallet.AccountsRoot.SingleOrDefault();
                        if (acc == null || acc.LastBlockSyncedHeight < heightAtDate)
                        {
                            this.logger.LogDebug("The chain of headers has finished downloading, updating wallet '{0}' with height {1}", wallet.Name, heightAtDate);
                            this.UpdateLastBlockSyncedHeight(wallet, this.ChainIndexer.GetHeader(heightAtDate));
                            this.SaveWallet(wallet);
                        }
                    }
                },
                (ex) =>
                {
                    // In case of an exception while waiting for the chain to be at a certain height, we just cut our losses and
                    // sync from the current height.
                    this.logger.LogError($"Exception occurred while waiting for chain to download: {ex.Message}");

                    foreach (Types.Wallet wallet in wallets)
                    {
                        this.UpdateLastBlockSyncedHeight(wallet, this.ChainIndexer.Tip);
                    }
                },
                TimeSpans.FiveSeconds);
        }

        /// <inheritdoc />
        public ExtKey GetExtKey(WalletAccountReference accountReference, string password = "", bool cache = false)
        {
            Types.Wallet wallet = this.GetWalletByName(accountReference.WalletName);
            string cacheKey = wallet.EncryptedSeed;
            Key privateKey;

            if (this.privateKeyCache.TryGetValue(cacheKey, out SecureString secretValue))
            {
                privateKey = wallet.Network.CreateBitcoinSecret(secretValue.FromSecureString()).PrivateKey;
            }
            else
            {
                privateKey = Key.Parse(wallet.EncryptedSeed, password, wallet.Network);
            }

            if (cache)
            {
                // The default duration the secret is cached is 5 minutes.
                var timeOutDuration = new TimeSpan(0, 5, 0);
                this.UnlockWallet(password, accountReference.WalletName, (int)timeOutDuration.TotalSeconds);
            }

            return new ExtKey(privateKey, wallet.ChainCode);
        }

        /// <inheritdoc />
        public IEnumerable<string> Sweep(IEnumerable<string> privateKeys, string destAddress, bool broadcast)
        {
            // Build the set of scriptPubKeys to look for.
            var scriptList = new HashSet<Script>();

            var keyMap = new Dictionary<Script, Key>();

            // Currently this is only designed to support P2PK and P2PKH, although segwit scripts are probably easily added.
            foreach (string wif in privateKeys)
            {
                var privateKey = Key.Parse(wif, this.network);

                Script p2pk = PayToPubkeyTemplate.Instance.GenerateScriptPubKey(privateKey.PubKey);
                Script p2pkh = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(privateKey.PubKey);

                keyMap.Add(p2pk, privateKey);
                keyMap.Add(p2pkh, privateKey);

                scriptList.Add(p2pk);
                scriptList.Add(p2pkh);
            }

            var coinView = this.utxoIndexer.GetCoinviewAtHeight(this.ChainIndexer.Height);

            var builder = new TransactionBuilder(this.network);

            var sweepTransactions = new List<string>();

            Money total = 0;
            int currentOutputCount = 0;

            foreach (OutPoint outPoint in coinView.UnspentOutputs)
            {
                // Obtain the transaction output in question.
                TxOut txOut = coinView.Transactions[outPoint.Hash].Outputs[outPoint.N];

                // Check if the scriptPubKey matches one of those for the supplied private keys.
                if (!scriptList.Contains(txOut.ScriptPubKey))
                {
                    continue;
                }

                // Add the UTXO as an input to the sweeping transaction.
                builder.AddCoins(new Coin(outPoint, txOut));
                builder.AddKeys(new[] { keyMap[txOut.ScriptPubKey] });

                currentOutputCount++;
                total += txOut.Value;

                if (total == 0)
                {
                    continue;
                }

                // If we reach a high total output count, we'll finalize the transaction and start building another one.
                if (currentOutputCount > 500)
                {
                    PrepareTransaction(destAddress, ref builder, total, sweepTransactions);

                    currentOutputCount = 0;
                    total = 0;
                }
            }

            // If there was a total of less than 500 inputs, or leftovers, we'll prepare the transaction.
            if (currentOutputCount > 0)
            {
                PrepareTransaction(destAddress, ref builder, total, sweepTransactions);
            }

            if (broadcast)
            {
                foreach (string sweepTransaction in sweepTransactions)
                {
                    Transaction toBroadcast = this.network.CreateTransaction(sweepTransaction);

                    this.broadcasterManager.BroadcastTransactionAsync(toBroadcast).GetAwaiter().GetResult();
                }
            }

            return sweepTransactions;
        }

        public void PrepareTransaction(string destAddress, ref TransactionBuilder builder, Money total, List<string> sweepTransactions)
        {
            BitcoinAddress destination = BitcoinAddress.Create(destAddress, this.network);

            builder.Send(destination, total);

            // Cause the last destination to pay the fee, as we have no other funds to pay fees with.
            builder.SubtractFees();

            FeeRate feeRate = this.walletFeePolicy.GetFeeRate(FeeType.High.ToConfirmations());
            builder.SendEstimatedFees(feeRate);

            Transaction sweepTransaction = builder.BuildTransaction(true);

            TransactionPolicyError[] errors = builder.Check(sweepTransaction);

            if (errors.Length == 0)
            {
                sweepTransactions.Add(sweepTransaction.ToHex());
            }
            else
            {
                // If there are errors, simply append them to the list of return values.
                foreach (var error in errors)
                {
                    sweepTransactions.Add(error.ToString());
                }
            }

            // Reset the builder and related state, as we are now creating a fresh transaction.
            builder = new TransactionBuilder(this.network);
        }
    }
}