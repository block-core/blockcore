using System.Collections.Generic;
using Blockcore.NBitcoin;

namespace Blockcore.Features.Wallet.Api.Models
{
    public sealed class AddressGroupingModel
    {
        public AddressGroupingModel()
        {
            this.AddressGroups = new List<AddressGroupModel>();
        }

        public List<AddressGroupModel> AddressGroups { get; set; }
    }

    public sealed class AddressGroupModel
    {
        public string Address { get; set; }
        public Money Amount { get; set; }
    }
}
