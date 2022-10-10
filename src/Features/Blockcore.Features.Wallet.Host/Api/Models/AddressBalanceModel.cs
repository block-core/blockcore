﻿using NBitcoin;
using Newtonsoft.Json;

namespace Blockcore.Features.Wallet.Api.Models
{
    public class AddressBalanceModel
    {
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }

        [JsonProperty(PropertyName = "coinType")]
        public int CoinType { get; set; }

        [JsonProperty(PropertyName = "amountConfirmed")]
        public Money AmountConfirmed { get; set; }

        [JsonProperty(PropertyName = "amountUnconfirmed")]
        public Money AmountUnconfirmed { get; set; }

        [JsonProperty(PropertyName = "spendableAmount")]
        public Money SpendableAmount { get; set; }
    }
}
