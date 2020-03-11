using System.Collections.Generic;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Consensus
{
    public class UnspentOutputsComparer : IComparer<UnspentOutput>
    {
        public static UnspentOutputsComparer Instance { get; } = new UnspentOutputsComparer();

        private readonly OutPointComparer Comparer = new OutPointComparer();

        public int Compare(UnspentOutput x, UnspentOutput y)
        {
            return this.Comparer.Compare(x.OutPoint, y.OutPoint);
        }
    }
}
