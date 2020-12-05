using System.Collections.Generic;
using Blockcore.Consensus.BlockInfo;
using NBitcoin;

namespace Blockcore.P2P.Protocol.Payloads
{
    /// <summary>
    /// Proven headers payload which contains list of up to 2000 proven headers.
    /// </summary>
    /// <seealso cref="Payload" />
    [Payload("provhdr")]
    public class ProvenHeadersPayload : Payload
    {
        /// <summary>
        /// <see cref="Headers"/>
        /// </summary>
        private List<ProvenBlockHeader> headers = new List<ProvenBlockHeader>();

        /// <summary>
        /// Gets a list of up to 2,000 proven headers.
        /// </summary>
        public List<ProvenBlockHeader> Headers => this.headers;

        public ProvenHeadersPayload()
        {
        }

        public ProvenHeadersPayload(params ProvenBlockHeader[] headers)
        {
            this.Headers.AddRange(headers);
        }

        /// <inheritdoc />
        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.headers);
        }
    }
}