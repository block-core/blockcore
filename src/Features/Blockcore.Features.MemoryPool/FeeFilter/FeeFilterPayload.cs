using Blockcore.NBitcoin;
using Blockcore.P2P.Protocol.Payloads;

namespace Blockcore.Features.MemoryPool.FeeFilter
{
    [Payload("feefilter")]
    public class FeeFilterPayload : Payload
    {
        private long feeFilter;

        public long NewFeeFilter
        {
            get => this.feeFilter;
            set => this.feeFilter = value;
        }

        public FeeFilterPayload()
        {
        }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.feeFilter);
        }
    }
}