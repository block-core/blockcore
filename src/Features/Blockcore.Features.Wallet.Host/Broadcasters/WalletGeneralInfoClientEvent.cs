using System;
using System.Collections.Generic;
using Blockcore.Broadcasters;
using Blockcore.EventBus;
using Blockcore.Features.Wallet.Api.Models;

namespace Blockcore.Features.Wallet.Broadcasters
{
    public class WalletGeneralInfoClientEvent : EventBase
    {
        public WalletGeneralInfoModel WalletInfo { get; set; }

        public string WalletName { get; set; }

        public IEnumerable<AccountBalanceModel> AccountsBalances { get; set; }
    }
}