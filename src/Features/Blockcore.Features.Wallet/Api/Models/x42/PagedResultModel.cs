using System.Collections.Generic;

namespace Blockcore.Features.Wallet.Api.Models.X42
{
    public class PagedResultModel<T>
    {
        public int TotalCount { get; set; }
        public List<T> Data { get; set; }

        public PagedResultModel()
        {

        }
    }
}
