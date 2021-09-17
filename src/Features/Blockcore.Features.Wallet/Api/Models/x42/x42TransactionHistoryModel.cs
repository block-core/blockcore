using System;
using NBitcoin;

namespace Blockcore.Features.Wallet.Api.Models.X42
{
    public class x42TransactionHistoryModel
    {
        public string Type { get; set; }
        public Money Amount { get; set; }
        public string TransactionTime { get; set; }

        public x42TransactionHistoryModel()
        {

        }

        public x42TransactionHistoryModel(TransactionItemModel transactionItemModel)
        {
            this.Type = transactionItemModel.Type.ToString();
            this.TransactionTime = transactionItemModel.Timestamp.DateTime.ToString(("yyyy-MM-ddTHH:mm:ss"));
            this.Amount = transactionItemModel.Amount;
        }

    }
}
