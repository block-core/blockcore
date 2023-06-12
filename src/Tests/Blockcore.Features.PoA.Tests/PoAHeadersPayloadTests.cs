using System.Collections.Generic;
using System.IO;
using Blockcore.Features.PoA.Payloads;
using Blockcore.NBitcoin;
using Xunit;

namespace Blockcore.Features.PoA.Tests
{
    public class PoAHeadersPayloadTests
    {
        [Fact]
        public void CanSerializeAndDeserialize()
        {
            var factory = new PoAConsensusFactory();

            var headers = new List<PoABlockHeader>()
            {
                new PoABlockHeader() { Version = 1 },
                new PoABlockHeader() { Version = 2 },
                new PoABlockHeader() { Version = 3 }
            };

            var payload = new PoAHeadersPayload(headers);

            // Serialize.
            var memoryStream = new MemoryStream();
            var bitcoinSerializeStream = new BitcoinStream(memoryStream, true, factory);

            payload.ReadWriteCore(bitcoinSerializeStream);

            // Deserialize.
            memoryStream.Seek(0, SeekOrigin.Begin);
            var bitcoinDeserializeStream = new BitcoinStream(memoryStream, false, factory);

            var deserializedPayload = new PoAHeadersPayload();
            deserializedPayload.ReadWriteCore(bitcoinDeserializeStream);

            Assert.Equal(payload.Headers.Count, deserializedPayload.Headers.Count);
        }
    }
}