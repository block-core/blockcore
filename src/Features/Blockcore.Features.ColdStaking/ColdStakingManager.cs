using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Blockcore.AsyncWork;
using Blockcore.Configuration;
using Blockcore.Connection.Broadcasting;
using Blockcore.Consensus.BlockInfo;
using Blockcore.Consensus.Chain;
using Blockcore.Consensus.ScriptInfo;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Features.BlockStore;
using Blockcore.Features.ColdStaking.Api.Controllers;
using Blockcore.Features.ColdStaking.Api.Models;
using Blockcore.Features.Wallet;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Features.Wallet.Types;
using Blockcore.Interfaces;
using Blockcore.Networks;
using Blockcore.Signals;
using Blockcore.Utilities;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.BuilderExtensions;

[assembly: InternalsVisibleTo("Blockcore.Features.ColdStaking.Tests")]
[assembly: InternalsVisibleTo("Blockcore.IntegrationTests")]

namespace Blockcore.Features.ColdStaking
{
    /// <summary>
    /// The manager class for implementing cold staking as covered in more detail in the remarks of
    /// the <see cref="ColdStakingFeature"/> class.
    /// This class provides the methods used by the <see cref="ColdStakingController"/>
    /// which in turn provides the API methods for accessing this functionality.
    /// </summary>
    /// <remarks>
    /// The following functionality is implemented in this class:
    /// <list type="bullet">
    /// <item><description>Generating cold staking address via the <see cref="GetFirstUnusedColdStakingAddress"/> method. These
    /// addresses are used for generating the cold staking setup.</description></item>
    /// <item><description>Creating a build context for generating the cold staking setup via the <see
    /// cref="GetColdStakingSetupTransaction"/> method.</description></item>
    /// </list>
    /// </remarks>
    public class ColdStakingManager : WalletManager, IWalletManager
    {
        private static Func<HdAccount, bool> coldStakingAccounts = a => a.Index >= Wallet.Types.Wallet.SpecialPurposeAccountIndexesStart;

        /// <summary>The account index of the cold wallet account.</summary>
        internal const int ColdWalletAccountIndex = Wallet.Types.Wallet.SpecialPurposeAccountIndexesStart + 0;

        /// <summary>The account name of the cold wallet account.</summary>
        internal const string ColdWalletAccountName = "coldStakingColdAddresses";

        /// <summary>The account index of the hot wallet account.</summary>
        internal const int HotWalletAccountIndex = Wallet.Types.Wallet.SpecialPurposeAccountIndexesStart + 1;

        /// <summary>The account name of the hot wallet account.</summary>
        internal const string HotWalletAccountName = "coldStakingHotAddresses";

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Provider of time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>
        /// Constructs the cold staking manager which is used by the cold staking controller.
        /// </summary>
        /// <param name="network">The network that the manager is running on.</param>
        /// <param name="chainIndexer">Thread safe class representing a chain of headers from genesis.</param>
        /// <param name="walletSettings">The wallet settings.</param>
        /// <param name="dataFolder">Contains path locations to folders and files on disk.</param>
        /// <param name="walletFeePolicy">The wallet fee policy.</param>
        /// <param name="asyncProvider">Factory for creating and also possibly starting application defined tasks inside async loop.</param>
        /// <param name="nodeLifeTime">Allows consumers to perform cleanup during a graceful shutdown.</param>
        /// <param name="scriptAddressReader">A reader for extracting an address from a <see cref="Script"/>.</param>
        /// <param name="loggerFactory">The logger factory to use to create the custom logger.</param>
        /// <param name="dateTimeProvider">Provider of time functions.</param>
        /// <param name="broadcasterManager">The broadcaster manager.</param>
        public ColdStakingManager(
            Network network,
            ChainIndexer chainIndexer,
            WalletSettings walletSettings,
            DataFolder dataFolder,
            IWalletFeePolicy walletFeePolicy,
            IAsyncProvider asyncProvider,
            INodeLifetime nodeLifeTime,
            IScriptAddressReader scriptAddressReader,
            ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider,
            ISignals signals = null,
            IBroadcasterManager broadcasterManager = null,
            IUtxoIndexer utxoIndexer = null) : base(
                loggerFactory,
                network,
                chainIndexer,
                walletSettings,
                dataFolder,
                walletFeePolicy,
                asyncProvider,
                nodeLifeTime,
                dateTimeProvider,
                scriptAddressReader,
                signals,
                broadcasterManager,
                utxoIndexer)
        {
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));

            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Overrides the default <see cref="WalletManager.CreateAddressFromScriptLookup"/>.
        /// </summary>
        /// <returns>A new <see cref="ColdStakingAddressLookup"/> object for use by this class.</returns>
        protected override ScriptToAddressLookup CreateAddressFromScriptLookup()
        {
            return new ColdStakingAddressLookup(this.network);
        }

        /// <inheritdoc />
        public override Dictionary<string, ScriptTemplate> GetValidStakingTemplates()
        {
            Dictionary<string, ScriptTemplate> templates = base.GetValidStakingTemplates();
            templates["ColdStaking"] = ColdStakingScriptTemplate.Instance;
            return templates;
        }

        // <inheritdoc />
        public override IEnumerable<BuilderExtension> GetTransactionBuilderExtensionsForStaking()
        {
            return base.GetTransactionBuilderExtensionsForStaking().Concat(new List<BuilderExtension> { new ColdStakingBuilderExtension(true) });
        }

        /// <summary>
        /// Gets all the unspent transactions in a wallet from the accounts participating in staking.
        /// </summary>
        /// <param name="walletName">Name of the wallet to get the transactions from.</param>
        /// <param name="confirmations">Number of confirmation required.</param>
        /// <returns>An enumeration of <see cref="UnspentOutputReference"/> objects.</returns>
        public override IEnumerable<UnspentOutputReference> GetSpendableTransactionsInWalletForStaking(string walletName, int confirmations = 0)
        {
            return this.GetUnspentTransactionsInWallet(walletName, confirmations,
                a => (a.Index < Wallet.Types.Wallet.SpecialPurposeAccountIndexesStart) || (a.Index == ColdStakingManager.HotWalletAccountIndex));
        }

        /// <summary>
        /// Returns information related to cold staking.
        /// </summary>
        /// <param name="walletName">The wallet to return the information for.</param>
        /// <returns>A <see cref="GetColdStakingInfoResponse"/> object containing the information.</returns>
        public GetColdStakingInfoResponse GetColdStakingInfo(string walletName)
        {
            Wallet.Types.Wallet wallet = this.GetWalletByName(walletName);

            var response = new GetColdStakingInfoResponse()
            {
                ColdWalletAccountExists = this.GetColdStakingAccount(wallet, true) != null,
                HotWalletAccountExists = this.GetColdStakingAccount(wallet, false) != null
            };

            this.logger.LogTrace("(-):'{0}'", response);
            return response;
        }

        /// <summary>
        /// Gets a cold staking account.
        /// </summary>
        /// <remarks>
        /// <para>In order to keep track of cold staking addresses and balances we are using <see cref="HdAccount"/>'s
        /// with indexes starting from the value defined in <see cref="Wallet.SpecialPurposeAccountIndexesStart"/>.
        /// </para><para>
        /// We are using two such accounts, one when the wallet is in the role of cold wallet, and another one when
        /// the wallet is in the role of hot wallet. For this reason we specify the required account when calling this
        /// method.
        /// </para></remarks>
        /// <param name="wallet">The wallet where we wish to create the account.</param>
        /// <param name="isColdWalletAccount">Indicates whether we need the cold wallet account (versus the hot wallet account).</param>
        /// <returns>The cold staking account or <c>null</c> if the account does not exist.</returns>
        public HdAccount GetColdStakingAccount(Wallet.Types.Wallet wallet, bool isColdWalletAccount)
        {
            var coinType = wallet.Network.Consensus.CoinType;
            HdAccount account = wallet.GetAccount(isColdWalletAccount ? ColdWalletAccountName : HotWalletAccountName);
            if (account == null)
            {
                this.logger.LogTrace("(-)[ACCOUNT_DOES_NOT_EXIST]:null");
                return null;
            }

            this.logger.LogTrace("(-):'{0}'", account.Name);
            return account;
        }

        /// <summary>
        /// Creates a cold staking account and ensures that it has at least one address.
        /// If the account already exists then the existing account is returned.
        /// </summary>
        /// <remarks>
        /// <para>In order to keep track of cold staking addresses and balances we are using <see cref="HdAccount"/>'s
        /// with indexes starting from the value defined in <see cref="Wallet.SpecialPurposeAccountIndexesStart"/>.
        /// </para><para>
        /// We are using two such accounts, one when the wallet is in the role of cold wallet, and another one when
        /// the wallet is in the role of hot wallet. For this reason we specify the required account when calling this
        /// method.
        /// </para></remarks>
        /// <param name="walletName">The name of the wallet where we wish to create the account.</param>
        /// <param name="isColdWalletAccount">Indicates whether we need the cold wallet account (versus the hot wallet account).</param>
        /// <param name="walletPassword">The wallet password which will be used to create the account.</param>
        /// <returns>The new or existing cold staking account.</returns>
        public HdAccount GetOrCreateColdStakingAccount(string walletName, bool isColdWalletAccount, string walletPassword)
        {
            Wallet.Types.Wallet wallet = this.GetWalletByName(walletName);

            HdAccount account = this.GetColdStakingAccount(wallet, isColdWalletAccount);
            if (account != null)
            {
                this.logger.LogTrace("(-)[ACCOUNT_ALREADY_EXIST]:'{0}'", account.Name);
                return account;
            }

            this.logger.LogDebug("The {0} wallet account for '{1}' does not exist and will now be created.", isColdWalletAccount ? "cold" : "hot", wallet.Name);

            int accountIndex;
            string accountName;

            if (isColdWalletAccount)
            {
                accountIndex = ColdWalletAccountIndex;
                accountName = ColdWalletAccountName;
            }
            else
            {
                accountIndex = HotWalletAccountIndex;
                accountName = HotWalletAccountName;
            }

            account = wallet.AddNewAccount(walletPassword, this.dateTimeProvider.GetTimeOffset(), accountIndex, accountName);

            // Maintain at least one unused address at all times. This will ensure that wallet recovery will also work.
            IEnumerable<HdAddress> newAddresses = account.CreateAddresses(wallet.Network, 1, false);
            this.UpdateKeysLookup(wallet, newAddresses);

            // Save the changes to the file.
            this.SaveWallet(wallet);

            this.logger.LogTrace("(-):'{0}'", account.Name);
            return account;
        }

        /// <summary>
        /// Add cold staking cold account when a wallet is recovered, this means every recovered wallet will automatical
        /// track addresses under the <see cref="ColdWalletAccountIndex"/> HD account.
        /// </summary>
        /// <inheritdoc />
        public override Wallet.Types.Wallet RecoverWallet(string password, string name, string mnemonic, DateTime creationTime, string passphrase, int? coinType = null)
        {
            Wallet.Types.Wallet wallet = base.RecoverWallet(password, name, mnemonic, creationTime, passphrase, coinType);

            this.GetOrCreateColdStakingAccount(wallet.Name, false, password);
            this.GetOrCreateColdStakingAccount(wallet.Name, true, password);

            return wallet;
        }

        /// <summary>
        /// Gets the first unused cold staking address. Creates a new address if required.
        /// </summary>
        /// <param name="walletName">The name of the wallet providing the cold staking address.</param>
        /// <param name="isColdWalletAddress">Indicates whether we need the cold wallet address (versus the hot wallet address).</param>
        /// <returns>The cold staking address or <c>null</c> if the required account does not exist.</returns>
        internal HdAddress GetFirstUnusedColdStakingAddress(string walletName, bool isColdWalletAddress)
        {
            Guard.NotNull(walletName, nameof(walletName));

            Wallet.Types.Wallet wallet = this.GetWalletByName(walletName);
            HdAccount account = this.GetColdStakingAccount(wallet, isColdWalletAddress);
            if (account == null)
            {
                this.logger.LogTrace("(-)[ACCOUNT_DOES_NOT_EXIST]:null");
                return null;
            }

            HdAddress address = account.GetFirstUnusedReceivingAddress(wallet.walletStore);
            if (address == null)
            {
                this.logger.LogDebug("No unused address exists on account '{0}'. Adding new address.", account.Name);
                IEnumerable<HdAddress> newAddresses = account.CreateAddresses(wallet.Network, 1);
                this.UpdateKeysLookup(wallet, newAddresses);
                address = newAddresses.First();
            }

            this.logger.LogTrace("(-):'{0}'", address.Address);
            return address;
        }

        /// <summary>
        /// Creates cold staking setup <see cref="Transaction"/>.
        /// </summary>
        /// <remarks>
        /// The <paramref name="coldWalletAddress"/> and <paramref name="hotWalletAddress"/> would be expected to be
        /// from different wallets and typically also different physical machines under normal circumstances. The following
        /// rules are enforced by this method and would lead to a <see cref="WalletException"/> otherwise:
        /// <list type="bullet">
        /// <item><description>The cold and hot wallet addresses are expected to belong to different wallets.</description></item>
        /// <item><description>Either the cold or hot wallet address must belong to a cold staking account in the wallet identified
        /// by <paramref name="walletName"/></description></item>
        /// <item><description>The account specified in <paramref name="walletAccount"/> can't be a cold staking account.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="walletTransactionHandler">The wallet transaction handler. Contains the <see cref="WalletTransactionHandler.BuildTransaction"/> method.</param>
        /// <param name="coldWalletAddress">The cold wallet address generated by <see cref="GetColdStakingAddress"/>.</param>
        /// <param name="hotWalletAddress">The hot wallet address generated by <see cref="GetColdStakingAddress"/>.</param>
        /// <param name="walletName">The name of the wallet.</param>
        /// <param name="walletAccount">The wallet account.</param>
        /// <param name="walletPassword">The wallet password.</param>
        /// <param name="amount">The amount to cold stake.</param>
        /// <param name="feeAmount">The fee to pay for the cold staking setup transaction.</param>
        /// <param name="useSegwitChangeAddress">Use a segwit style change address.</param>
        /// <param name="payToScript">Indicate script staking (P2SH or P2WSH outputs).</param>
        /// <returns>The <see cref="Transaction"/> for setting up cold staking.</returns>
        /// <exception cref="WalletException">Thrown if any of the rules listed in the remarks section of this method are broken.</exception>
        internal Transaction GetColdStakingSetupTransaction(IWalletTransactionHandler walletTransactionHandler,
            string coldWalletAddress, string hotWalletAddress, string walletName, string walletAccount,
            string walletPassword, Money amount, Money feeAmount, bool useSegwitChangeAddress = false, bool payToScript = false)
        {
            Guard.NotNull(walletTransactionHandler, nameof(walletTransactionHandler));
            Guard.NotEmpty(coldWalletAddress, nameof(coldWalletAddress));
            Guard.NotEmpty(hotWalletAddress, nameof(hotWalletAddress));
            Guard.NotEmpty(walletName, nameof(walletName));
            Guard.NotEmpty(walletAccount, nameof(walletAccount));
            Guard.NotNull(amount, nameof(amount));
            Guard.NotNull(feeAmount, nameof(feeAmount));

            Wallet.Types.Wallet wallet = this.GetWalletByName(walletName);

            // Get/create the cold staking accounts.
            HdAccount coldAccount = this.GetOrCreateColdStakingAccount(walletName, true, walletPassword);
            HdAccount hotAccount = this.GetOrCreateColdStakingAccount(walletName, false, walletPassword);

            HdAddress coldAddress = coldAccount?.ExternalAddresses.FirstOrDefault(s => s.Address == coldWalletAddress || s.Bech32Address == coldWalletAddress);
            HdAddress hotAddress = hotAccount?.ExternalAddresses.FirstOrDefault(s => s.Address == hotWalletAddress || s.Bech32Address == hotWalletAddress);

            bool thisIsColdWallet = coldAddress != null;
            bool thisIsHotWallet = hotAddress != null;

            this.logger.LogDebug("Local wallet '{0}' does{1} contain cold wallet address '{2}' and does{3} contain hot wallet address '{4}'.",
                walletName, thisIsColdWallet ? "" : " NOT", coldWalletAddress, thisIsHotWallet ? "" : " NOT", hotWalletAddress);

            if (thisIsColdWallet && thisIsHotWallet)
            {
                this.logger.LogTrace("(-)[COLDSTAKE_BOTH_HOT_AND_COLD]");
                throw new WalletException("You can't use this wallet as both hot wallet and cold wallet.");
            }

            if (!thisIsColdWallet && !thisIsHotWallet)
            {
                this.logger.LogTrace("(-)[COLDSTAKE_ADDRESSES_NOT_IN_ACCOUNTS]");
                throw new WalletException("The hot and cold wallet addresses could not be found in the corresponding accounts.");
            }

            Script destination = null;
            KeyId hotPubKeyHash = null;
            KeyId coldPubKeyHash = null;

            // Check if this is a segwit address
            if (coldAddress?.Bech32Address == coldWalletAddress || hotAddress?.Bech32Address == hotWalletAddress)
            {
                hotPubKeyHash = new BitcoinWitPubKeyAddress(hotWalletAddress, wallet.Network).Hash.AsKeyId();
                coldPubKeyHash = new BitcoinWitPubKeyAddress(coldWalletAddress, wallet.Network).Hash.AsKeyId();
                destination = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKeyHash, coldPubKeyHash);

                if (payToScript)
                {
                    HdAddress address = coldAddress ?? hotAddress;
                    address.RedeemScript = destination;
                    destination = destination.WitHash.ScriptPubKey;
                }
            }
            else
            {
                hotPubKeyHash = new BitcoinPubKeyAddress(hotWalletAddress, wallet.Network).Hash;
                coldPubKeyHash = new BitcoinPubKeyAddress(coldWalletAddress, wallet.Network).Hash;
                destination = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKeyHash, coldPubKeyHash);

                if (payToScript)
                {
                    HdAddress address = coldAddress ?? hotAddress;
                    address.RedeemScript = destination;
                    destination = destination.Hash.ScriptPubKey;
                }
            }

            // Only normal accounts should be allowed.
            if (this.GetAccounts(walletName).All(a => a.Name != walletAccount))
            {
                this.logger.LogTrace("(-)[COLDSTAKE_ACCOUNT_NOT_FOUND]");
                throw new WalletException($"Can't find wallet account '{walletAccount}'.");
            }

            var context = new TransactionBuildContext(wallet.Network)
            {
                AccountReference = new WalletAccountReference(walletName, walletAccount),
                TransactionFee = feeAmount,
                MinConfirmations = 0,
                Shuffle = false,
                UseSegwitChangeAddress = useSegwitChangeAddress,
                WalletPassword = walletPassword,
                Recipients = new List<Recipient>() { new Recipient { Amount = amount, ScriptPubKey = destination } }
            };

            if (payToScript)
            {
                // In the case of P2SH and P2WSH, to avoid the possibility of lose track of funds
                // we add an opreturn with the hot and cold key hashes to the setup transaction
                // this will allow a user to recreate the redeem script of the output in case they lose
                // access to one of the keys.
                // The special marker will help a wallet that is tracking cold staking accounts to monitor
                // the hot and cold keys, if a special marker is found then the keys are in the opreturn are checked
                // against the current wallet, if found and validated the wallet will track that ScriptPubKey

                var opreturnKeys = new List<byte>();
                opreturnKeys.AddRange(hotPubKeyHash.ToBytes());
                opreturnKeys.AddRange(coldPubKeyHash.ToBytes());

                context.OpReturnRawData = opreturnKeys.ToArray();
                TxNullDataTemplate template = this.network.StandardScriptsRegistry.GetScriptTemplates.OfType<TxNullDataTemplate>().First();
                if (template.MinRequiredSatoshiFee > 0)
                    context.OpReturnAmount = Money.Satoshis(template.MinRequiredSatoshiFee); // mandatory fee must be paid.

                // The P2SH and P2WSH hide the cold stake keys in the script hash so the wallet cannot track
                // the ouputs based on the derived keys when the trx is subbmited to the network.
                // So we add the output script manually.
            }

            // Register the cold staking builder extension with the transaction builder.
            context.TransactionBuilder.Extensions.Add(new ColdStakingBuilderExtension(false));

            // Build the transaction.
            Transaction transaction = walletTransactionHandler.BuildTransaction(context);

            this.logger.LogTrace("(-)");
            return transaction;
        }

        /// <summary>
        /// Creates a cold staking withdrawal <see cref="Transaction"/>.
        /// </summary>
        /// <remarks>
        /// Cold staking withdrawal is performed on the wallet that is in the role of the cold staking cold wallet.
        /// </remarks>
        /// <param name="walletTransactionHandler">The wallet transaction handler used to build the transaction.</param>
        /// <param name="receivingAddress">The address that will receive the withdrawal.</param>
        /// <param name="walletName">The name of the wallet in the role of cold wallet.</param>
        /// <param name="walletPassword">The wallet password.</param>
        /// <param name="amount">The amount to remove from cold staking.</param>
        /// <param name="feeAmount">The fee to pay for cold staking transaction withdrawal.</param>
        /// <returns>The <see cref="Transaction"/> for cold staking withdrawal.</returns>
        /// <exception cref="WalletException">Thrown if the receiving address is in a cold staking account in this wallet.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the receiving address is invalid.</exception>
        internal Transaction GetColdStakingWithdrawalTransaction(IWalletTransactionHandler walletTransactionHandler, string receivingAddress,
            string walletName, string walletPassword, Money amount, Money feeAmount)
        {
            Guard.NotEmpty(receivingAddress, nameof(receivingAddress));
            Guard.NotEmpty(walletName, nameof(walletName));
            Guard.NotNull(amount, nameof(amount));
            Guard.NotNull(feeAmount, nameof(feeAmount));

            Wallet.Types.Wallet wallet = this.GetWalletByName(walletName);

            // Get the cold staking account.
            HdAccount coldAccount = this.GetColdStakingAccount(wallet, true);
            if (coldAccount == null)
            {
                this.logger.LogTrace("(-)[COLDSTAKE_ACCOUNT_DOES_NOT_EXIST]");
                throw new WalletException("The cold wallet account does not exist.");
            }

            // Prevent reusing cold stake addresses as regular withdrawal addresses.
            if (coldAccount.ExternalAddresses.Concat(coldAccount.InternalAddresses).Any(s => s.Address == receivingAddress || s.Bech32Address == receivingAddress))
            {
                this.logger.LogTrace("(-)[COLDSTAKE_INVALID_COLD_WALLET_ADDRESS_USAGE]");
                throw new WalletException("You can't send the money to a cold staking cold wallet account.");
            }

            HdAccount hotAccount = this.GetColdStakingAccount(wallet, false);
            if (hotAccount != null && hotAccount.ExternalAddresses.Concat(hotAccount.InternalAddresses).Any(s => s.Address == receivingAddress || s.Bech32Address == receivingAddress))
            {
                this.logger.LogTrace("(-)[COLDSTAKE_INVALID_HOT_WALLET_ADDRESS_USAGE]");
                throw new WalletException("You can't send the money to a cold staking hot wallet account.");
            }

            Script destination = null;

            if (BitcoinWitPubKeyAddress.IsValid(receivingAddress, this.network, out Exception _))
            {
                destination = new BitcoinWitPubKeyAddress(receivingAddress, wallet.Network).ScriptPubKey;
            }
            else
            {
                // Send the money to the receiving address.
                destination = BitcoinAddress.Create(receivingAddress, wallet.Network).ScriptPubKey;
            }

            // Create the transaction build context (used in BuildTransaction).
            var accountReference = new WalletAccountReference(walletName, coldAccount.Name);
            var context = new TransactionBuildContext(wallet.Network)
            {
                AccountReference = accountReference,
                // Specify a dummy change address to prevent a change (internal) address from being created.
                // Will be changed after the transacton is built and before it is signed.
                ChangeAddress = coldAccount.ExternalAddresses.First(),
                TransactionFee = feeAmount,
                MinConfirmations = 0,
                Shuffle = false,
                Sign = false,
                Recipients = new[] { new Recipient { Amount = amount, ScriptPubKey = destination } }.ToList()
            };

            // Register the cold staking builder extension with the transaction builder.
            context.TransactionBuilder.Extensions.Add(new ColdStakingBuilderExtension(false));

            // Avoid script errors due to missing scriptSig.
            context.TransactionBuilder.StandardTransactionPolicy.ScriptVerify = null;

            // Build the transaction according to the settings recorded in the context.
            Transaction transaction = walletTransactionHandler.BuildTransaction(context);

            // Map OutPoint to UnspentOutputReference.
            Dictionary<OutPoint, UnspentOutputReference> mapOutPointToUnspentOutput = this.GetSpendableTransactionsInAccount(accountReference)
                .ToDictionary(unspent => unspent.ToOutPoint(), unspent => unspent);

            // Set the cold staking scriptPubKey on the change output.
            TxOut changeOutput = transaction.Outputs.SingleOrDefault(output => (output.ScriptPubKey != destination) && (output.Value != 0));
            if (changeOutput != null)
            {
                // Find the largest input.
                TxIn largestInput = transaction.Inputs.OrderByDescending(input => mapOutPointToUnspentOutput[input.PrevOut].Transaction.Amount).Take(1).Single();

                // Set the scriptPubKey of the change output to the scriptPubKey of the largest input.
                changeOutput.ScriptPubKey = mapOutPointToUnspentOutput[largestInput.PrevOut].Transaction.ScriptPubKey;
            }

            // Add keys for signing inputs. This takes time so only add keys for distinct addresses.
            foreach (UnspentOutputReference item in transaction.Inputs.Select(i => mapOutPointToUnspentOutput[i.PrevOut]).Distinct())
            {
                Script prevscript = item.Transaction.ScriptPubKey;

                if (prevscript.IsScriptType(ScriptType.P2SH) || prevscript.IsScriptType(ScriptType.P2WSH))
                {
                    if (item.Address.RedeemScript == null)
                        throw new WalletException("Missing redeem script");

                    // Provide the redeem script to the builder
                    var scriptCoin = ScriptCoin.Create(this.network, item.ToOutPoint(), new TxOut(item.Transaction.Amount, prevscript), item.Address.RedeemScript);
                    context.TransactionBuilder.AddCoins(scriptCoin);
                }

                context.TransactionBuilder.AddKeys(wallet.GetExtendedPrivateKeyForAddress(walletPassword, item.Address));
            }

            // Sign the transaction.
            context.TransactionBuilder.SignTransactionInPlace(transaction);

            this.logger.LogTrace("(-):'{0}'", transaction.GetHash());
            return transaction;
        }

        /// <summary>
        /// Gets the spendable transactions associated with cold wallet addresses.
        /// </summary>
        /// <param name="walletName">The name of the wallet.</param>
        /// <param name="isColdWalletAccount">The cold staking account to get the transactions for.</param>
        /// <param name="confirmations">The number of confirmations.</param>
        /// <returns>An enumeration of <see cref="UnspentOutputReference"/> items.</returns>
        public IEnumerable<UnspentOutputReference> GetSpendableTransactionsInColdWallet(string walletName, bool isColdWalletAccount, int confirmations = 0)
        {
            Guard.NotEmpty(walletName, nameof(walletName));

            Wallet.Types.Wallet wallet = this.GetWalletByName(walletName);
            UnspentOutputReference[] res = null;
            lock (this.lockObject)
            {
                res = wallet.GetAllSpendableTransactions(wallet.walletStore, this.ChainIndexer.Tip.Height, confirmations,
                    a => a.Index == (isColdWalletAccount ? ColdWalletAccountIndex : HotWalletAccountIndex)).ToArray();
            }

            this.logger.LogTrace("(-):*.Count={0}", res.Count());
            return res;
        }

        /// <summary>
        /// Checks if the script contains a cold staking address and if so maintains the buffer.
        /// </summary>
        /// <param name="script">The script (possibly a cold staking script) to check.</param>
        /// <param name="accountFilter">The account filter.</param>
        public override void TransactionFoundInternal(Wallet.Types.Wallet wallet, Script script, Func<HdAccount, bool> accountFilter = null)
        {
            if (ColdStakingScriptTemplate.Instance.ExtractScriptPubKeyParameters(script, out KeyId hotPubKeyHash, out KeyId coldPubKeyHash))
            {
                base.TransactionFoundInternal(wallet, hotPubKeyHash.ScriptPubKey, a => a.Index == HotWalletAccountIndex);
                base.TransactionFoundInternal(wallet, coldPubKeyHash.ScriptPubKey, a => a.Index == ColdWalletAccountIndex);

                return;
            }
            else if (script.IsScriptType(ScriptType.P2SH) || script.IsScriptType(ScriptType.P2WSH))
            {
                if (this.walletIndex[wallet.Name].ScriptToAddressLookup.TryGetValue(script, out HdAddress address))
                {
                    if (ColdStakingScriptTemplate.Instance.ExtractScriptPubKeyParameters(address.RedeemScript, out hotPubKeyHash, out coldPubKeyHash))
                    {
                        base.TransactionFoundInternal(wallet, hotPubKeyHash.ScriptPubKey, a => a.Index == HotWalletAccountIndex);
                        base.TransactionFoundInternal(wallet, coldPubKeyHash.ScriptPubKey, a => a.Index == ColdWalletAccountIndex);

                        return;
                    }
                }
            }

            base.TransactionFoundInternal(wallet, script, accountFilter);
        }

        /// <summary>
        /// The purpose of this method is to try to identify the P2SH and P2WSH that are coldstake outputs for this wallet
        /// We look for an opreturn script that is created when seting up a P2SH and P2WSH cold stake trx
        /// if we find any then try to find the keys and track the script before calling in to the main wallet.
        /// </summary>
        /// <inheritdoc/>
        public override bool ProcessTransaction(Transaction transaction, int? blockHeight = null, Block block = null, bool isPropagated = true)
        {
            foreach (TxOut utxo in transaction.Outputs)
            {
                Script script = utxo.ScriptPubKey;

                if (script.IsUnspendable)
                {
                    var data = TxNullDataTemplate.Instance.ExtractScriptPubKeyParameters(script);
                    if (data != null && data.Length == 1)
                    {
                        if (data[0].Length == 40)
                        {
                            HdAddress address = null;

                            Span<byte> span = data[0].AsSpan();
                            var hotPubKey = new KeyId(span.Slice(0, 20).ToArray());
                            var coldPubKey = new KeyId(span.Slice(20, 20).ToArray());

                            foreach (KeyValuePair<string, WalletIndex> walletIndexItem in this.walletIndex)
                            {
                                if (walletIndexItem.Value.ScriptToAddressLookup.TryGetValue(hotPubKey.ScriptPubKey, out address)
                                || walletIndexItem.Value.ScriptToAddressLookup.TryGetValue(coldPubKey.ScriptPubKey, out address))
                                {
                                    Script destination = ColdStakingScriptTemplate.Instance.GenerateScriptPubKey(hotPubKey, coldPubKey);
                                    address.RedeemScript = destination;

                                    // Find the type of script for the opreturn (P2SH or P2WSH)
                                    foreach (TxOut utxoInner in transaction.Outputs)
                                    {
                                        if (utxoInner.ScriptPubKey == destination.Hash.ScriptPubKey)
                                        {
                                            if (!walletIndexItem.Value.ScriptToAddressLookup.TryGetValue(destination.Hash.ScriptPubKey, out HdAddress _))
                                                walletIndexItem.Value.ScriptToAddressLookup[destination.Hash.ScriptPubKey] = address;

                                            break;
                                        }

                                        if (utxoInner.ScriptPubKey == destination.WitHash.ScriptPubKey)
                                        {
                                            if (!walletIndexItem.Value.ScriptToAddressLookup.TryGetValue(destination.WitHash.ScriptPubKey, out HdAddress _))
                                                walletIndexItem.Value.ScriptToAddressLookup[destination.WitHash.ScriptPubKey] = address;

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return base.ProcessTransaction(transaction, blockHeight, block, isPropagated);
        }

        protected override void AddAddressToIndex(Wallet.Types.Wallet wallet, HdAddress address)
        {
            base.AddAddressToIndex(wallet, address);

            if (address.RedeemScript != null)
            {
                // The redeem script has no indication on the script type (P2SH or P2WSH),
                // so we track both, add both to the indexer then.

                if (!this.walletIndex[wallet.Name].ScriptToAddressLookup.TryGetValue(address.RedeemScript.Hash.ScriptPubKey, out HdAddress _))
                    this.walletIndex[wallet.Name].ScriptToAddressLookup[address.RedeemScript.Hash.ScriptPubKey] = address;

                if (!this.walletIndex[wallet.Name].ScriptToAddressLookup.TryGetValue(address.RedeemScript.WitHash.ScriptPubKey, out HdAddress _))
                    this.walletIndex[wallet.Name].ScriptToAddressLookup[address.RedeemScript.WitHash.ScriptPubKey] = address;
            }
        }
    }
}