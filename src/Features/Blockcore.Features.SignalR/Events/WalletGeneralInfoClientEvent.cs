using System;
using System.Collections.Generic;
using Blockcore.EventBus;
using Blockcore.Features.Wallet.Models;

namespace Blockcore.Features.SignalR.Events
{
    /// <summary>
    /// Marker type for Client
    /// </summary>
    public class WalletGeneralInfo
    {
    }

    public class WalletGeneralInfoClientEvent : WalletGeneralInfoModel, IClientEvent
    {
        
        public string WalletName { get; set; }
        public Type NodeEventType => typeof(WalletGeneralInfo);
        
        public IEnumerable<AccountBalanceModel> AccountsBalances { get; set; }

        public void BuildFrom(EventBase @event)
        {
            throw new NotImplementedException();
        }
    }
}