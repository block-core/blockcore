using System;
using System.Collections.Generic;
using System.Linq;
using Blockcore.Features.Wallet.Api.Models;
using Blockcore.Features.Wallet.Database;
using Blockcore.Features.Wallet.Exceptions;
using Blockcore.Features.Wallet.Helpers;
using Blockcore.Features.Wallet.Interfaces;
using Blockcore.Features.Wallet.Types;
using Blockcore.Networks;
using NBitcoin;

namespace Blockcore.Features.Wallet.Api.Controllers
{
    /// <summary>
    /// A helper class to build complex wallet models.
    /// </summary>
    public static class WalletModelBuilder
    {
        public static Mnemonic GenerateMnemonic(string language = "English", int wordCount = 12)
        {
            Wordlist wordList;
            switch (language.ToLowerInvariant())
            {
                case "english":
                    wordList = Wordlist.English;
                    break;

                case "french":
                    wordList = Wordlist.French;
                    break;

                case "spanish":
                    wordList = Wordlist.Spanish;
                    break;

                case "japanese":
                    wordList = Wordlist.Japanese;
                    break;

                case "chinesetraditional":
                    wordList = Wordlist.ChineseTraditional;
                    break;

                case "chinesesimplified":
                    wordList = Wordlist.ChineseSimplified;
                    break;

                default:
                    throw new FormatException($"Invalid language '{language}'. Choices are: English, French, Spanish, Japanese, ChineseSimplified and ChineseTraditional.");
            }

            var count = (WordCount)wordCount;

            // generate the mnemonic
            var mnemonic = new Mnemonic(wordList, count);

            return mnemonic;
        }

        /// <summary>
        /// This method is an attempt to fetch history and its complex calculation directly from disk
        /// and avoid the need to fetch the entire history to memory
        /// </summary>
        public static WalletHistoryModel GetHistorySlim(IWalletManager walletManager, Network network, WalletHistoryRequest request)
        {
            if (!string.IsNullOrEmpty(request.Address))
            {
                throw new NotImplementedException("Search by address not implemented");
            }

            if (!string.IsNullOrEmpty(request.SearchQuery))
            {
                throw new NotImplementedException("Search by SearchQuery not implemented");
            }

            var model = new WalletHistoryModel();

            // Get a list of all the transactions found in an account (or in a wallet if no account is specified), with the addresses associated with them.
            IEnumerable<AccountHistorySlim> accountsHistory = walletManager.GetHistorySlim(request.WalletName, request.AccountName, skip: request.Skip ?? 0, take: request.Take ?? int.MaxValue);

            foreach (AccountHistorySlim accountHistory in accountsHistory)
            {
                var transactionItems = new List<TransactionItemModel>();

                foreach (FlatHistorySlim item in accountHistory.History)
                {
                    var modelItem = new TransactionItemModel
                    {
                        Type = item.Transaction.IsSent ? TransactionItemType.Send : TransactionItemType.Received,
                        ToAddress = item.Transaction.Address,
                        Amount = item.Transaction.IsSent == false ? item.Transaction.Amount : Money.Zero,
                        Id = item.Transaction.IsSent ? item.Transaction.SentTo : item.Transaction.OutPoint.Hash,
                        Timestamp = item.Transaction.CreationTime,
                        ConfirmedInBlock = item.Transaction.BlockHeight,
                        BlockIndex = item.Transaction.BlockIndex
                    };

                    if (item.Transaction.IsSent == true) // handle send entries
                    {
                        // First we look for staking transaction as they require special attention.
                        // A staking transaction spends one of our inputs into 2 outputs or more, paid to the same address.
                        if ((item.Transaction.IsCoinStake ?? false == true))
                        {
                            if (item.Transaction.IsSent == true)
                            {
                                modelItem.Type = TransactionItemType.Staked;
                                var amount = item.Transaction.SentPayments.Sum(p => p.Amount);
                                modelItem.Amount = amount - item.Transaction.Amount;
                            }
                            else
                            {
                                // We don't show in history transactions that are outputs of staking transactions.
                                continue;
                            }
                        }
                        else
                        {
                            if (item.Transaction.SentPayments.All(a => a.PayToSelf == true))
                            {
                                // if all outputs are to ourself
                                // we don't show that in history
                                continue;
                            }

                            modelItem.Amount = item.Transaction.SentPayments.Where(x => x.PayToSelf == false).Sum(p => p.Amount);
                        }
                    }
                    else // handle receive entries
                    {
                        if (item.Address.IsChangeAddress())
                        {
                            // we don't display transactions sent to self
                            continue;
                        }

                        if (item.Transaction.IsCoinStake.HasValue && item.Transaction.IsCoinStake.Value == true)
                        {
                            // We don't show in history transactions that are outputs of staking transactions.
                            continue;
                        }
                    }

                    transactionItems.Add(modelItem);
                }

                model.AccountsHistoryModel.Add(new AccountHistoryModel
                {
                    TransactionsHistory = transactionItems,
                    Name = accountHistory.Account.Name,
                    CoinType = network.Consensus.CoinType,
                    HdPath = accountHistory.Account.HdPath
                });
            }

            return model;
        }

        public static WalletHistoryModel GetHistory(IWalletManager walletManager, Network network, WalletHistoryRequest request)
        {
            var model = new WalletHistoryModel();

            // Get a list of all the transactions found in an account (or in a wallet if no account is specified), with the addresses associated with them.
            IEnumerable<AccountHistory> accountsHistory = walletManager.GetHistory(request.WalletName, request.AccountName);

            foreach (AccountHistory accountHistory in accountsHistory)
            {
                var transactionItems = new List<TransactionItemModel>();
                var uniqueProcessedTxIds = new HashSet<uint256>();

                IEnumerable<FlatHistory> query = accountHistory.History;

                if (!string.IsNullOrEmpty(request.Address))
                {
                    query = query.Where(x => x.Address.Address == request.Address || x.Address.Bech32Address == request.Address);
                }

                // Sorting the history items by descending dates. That includes received and sent dates.
                List<FlatHistory> items = query
                    .OrderBy(o => o.Transaction.IsConfirmed() ? 1 : 0)
                    .ThenByDescending(o => o.Transaction.SpendingDetails?.CreationTime ?? o.Transaction.CreationTime)
                    .ToList();

                // Represents a sublist containing only the transactions that have already been spent.
                List<FlatHistory> spendingDetails = items.Where(t => t.Transaction.SpendingDetails != null).ToList();

                // Represents a sublist of transactions associated with receive addresses + a sublist of already spent transactions associated with change addresses.
                // In effect, we filter out 'change' transactions that are not spent, as we don't want to show these in the history.
                List<FlatHistory> history = items.Where(t => !t.Address.IsChangeAddress() || (t.Address.IsChangeAddress() && t.Transaction.IsSpent())).ToList();

                // Represents a sublist of 'change' transactions.
                List<FlatHistory> allchange = items.Where(t => t.Address.IsChangeAddress()).ToList();

                foreach (FlatHistory item in history)
                {
                    // Count only unique transactions and limit it to MaxHistoryItemsPerAccount.
                    int processedTransactions = uniqueProcessedTxIds.Count;
                    if (processedTransactions >= WalletController.MaxHistoryItemsPerAccount)
                    {
                        break;
                    }

                    TransactionOutputData transaction = item.Transaction;
                    HdAddress address = item.Address;

                    // First we look for staking transaction as they require special attention.
                    // A staking transaction spends one of our inputs into 2 outputs or more, paid to the same address.
                    if (transaction.SpendingDetails?.IsCoinStake != null && transaction.SpendingDetails.IsCoinStake.Value)
                    {
                        // We look for the output(s) related to our spending input.
                        List<FlatHistory> relatedOutputs = items.Where(h => h.Transaction.Id == transaction.SpendingDetails.TransactionId && h.Transaction.IsCoinStake != null && h.Transaction.IsCoinStake.Value).ToList();
                        if (relatedOutputs.Any())
                        {
                            // Add staking transaction details.
                            // The staked amount is calculated as the difference between the sum of the outputs and the input and should normally be equal to 1.
                            var stakingItem = new TransactionItemModel
                            {
                                Type = TransactionItemType.Staked,
                                ToAddress = address.Address,
                                Amount = relatedOutputs.Sum(o => o.Transaction.Amount) - transaction.Amount,
                                Id = transaction.SpendingDetails.TransactionId,
                                Timestamp = transaction.SpendingDetails.CreationTime,
                                ConfirmedInBlock = transaction.SpendingDetails.BlockHeight,
                                BlockIndex = transaction.SpendingDetails.BlockIndex
                            };

                            transactionItems.Add(stakingItem);
                            uniqueProcessedTxIds.Add(stakingItem.Id);
                        }

                        // No need for further processing if the transaction itself is the output of a staking transaction.
                        if (transaction.IsCoinStake != null)
                        {
                            continue;
                        }
                    }

                    // If this is a normal transaction (not staking) that has been spent, add outgoing fund transaction details.
                    if (transaction.SpendingDetails != null && transaction.SpendingDetails.IsCoinStake == null)
                    {
                        // Create a record for a 'send' transaction.
                        uint256 spendingTransactionId = transaction.SpendingDetails.TransactionId;
                        var sentItem = new TransactionItemModel
                        {
                            Type = TransactionItemType.Send,
                            Id = spendingTransactionId,
                            Timestamp = transaction.SpendingDetails.CreationTime,
                            ConfirmedInBlock = transaction.SpendingDetails.BlockHeight,
                            BlockIndex = transaction.SpendingDetails.BlockIndex,
                            Amount = Money.Zero
                        };

                        // If this 'send' transaction has made some external payments, i.e the funds were not sent to another address in the wallet.
                        if (transaction.SpendingDetails.Payments != null)
                        {
                            sentItem.Payments = new List<PaymentDetailModel>();
                            foreach (PaymentDetails payment in transaction.SpendingDetails.Payments)
                            {
                                sentItem.Payments.Add(new PaymentDetailModel
                                {
                                    DestinationAddress = payment.DestinationAddress,
                                    Amount = payment.Amount
                                });

                                sentItem.Amount += payment.Amount;
                            }
                        }

                        // Get the change address for this spending transaction.
                        FlatHistory changeAddress = allchange.FirstOrDefault(a => a.Transaction.Id == spendingTransactionId);

                        // Find all the spending details containing the spending transaction id and aggregate the sums.
                        // This is our best shot at finding the total value of inputs for this transaction.
                        var inputsAmount = new Money(spendingDetails.Where(t => t.Transaction.SpendingDetails.TransactionId == spendingTransactionId).Sum(t => t.Transaction.Amount));

                        // The fee is calculated as follows: funds in utxo - amount spent - amount sent as change.
                        sentItem.Fee = inputsAmount - sentItem.Amount - (changeAddress == null ? 0 : changeAddress.Transaction.Amount);

                        // Mined/staked coins add more coins to the total out.
                        // That makes the fee negative. If that's the case ignore the fee.
                        if (sentItem.Fee < 0)
                            sentItem.Fee = 0;

                        transactionItems.Add(sentItem);
                        uniqueProcessedTxIds.Add(sentItem.Id);
                    }

                    // We don't show in history transactions that are outputs of staking transactions.
                    if (transaction.IsCoinStake != null && transaction.IsCoinStake.Value && transaction.SpendingDetails == null)
                    {
                        continue;
                    }

                    // Create a record for a 'receive' transaction.
                    if (transaction.IsCoinStake == null && !address.IsChangeAddress())
                    {
                        // First check if we already have a similar transaction output, in which case we just sum up the amounts
                        TransactionItemModel existingReceivedItem = FindSimilarReceivedTransactionOutput(transactionItems, transaction);

                        if (existingReceivedItem == null)
                        {
                            // Add incoming fund transaction details.
                            var receivedItem = new TransactionItemModel
                            {
                                Type = TransactionItemType.Received,
                                ToAddress = address.Address,
                                Amount = transaction.Amount,
                                Id = transaction.Id,
                                Timestamp = transaction.CreationTime,
                                ConfirmedInBlock = transaction.BlockHeight,
                                BlockIndex = transaction.BlockIndex
                            };

                            transactionItems.Add(receivedItem);
                            uniqueProcessedTxIds.Add(receivedItem.Id);
                        }
                        else
                        {
                            existingReceivedItem.Amount += transaction.Amount;
                        }
                    }
                }

                transactionItems = transactionItems.Distinct(new SentTransactionItemModelComparer()).Select(e => e).ToList();

                // Sort and filter the history items.
                List<TransactionItemModel> itemsToInclude = transactionItems.OrderByDescending(t => t.Timestamp)
                    .Where(x => string.IsNullOrEmpty(request.SearchQuery) || (x.Id.ToString() == request.SearchQuery || x.ToAddress == request.SearchQuery || x.Payments.Any(p => p.DestinationAddress == request.SearchQuery)))
                    .Skip(request.Skip ?? 0)
                    .Take(request.Take ?? transactionItems.Count)
                    .ToList();

                model.AccountsHistoryModel.Add(new AccountHistoryModel
                {
                    TransactionsHistory = itemsToInclude,
                    Name = accountHistory.Account.Name,
                    CoinType = network.Consensus.CoinType,
                    HdPath = accountHistory.Account.HdPath
                });
            }

            return model;
        }

        private static TransactionItemModel FindSimilarReceivedTransactionOutput(List<TransactionItemModel> items, TransactionOutputData transaction)
        {
            TransactionItemModel existingTransaction = items.FirstOrDefault(i => i.Id == transaction.Id &&
                                                                                 i.Type == TransactionItemType.Received &&
                                                                                 i.ConfirmedInBlock == transaction.BlockHeight);
            return existingTransaction;
        }
    }
}