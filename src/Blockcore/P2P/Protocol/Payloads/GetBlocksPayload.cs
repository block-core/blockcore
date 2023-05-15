using Blockcore.NBitcoin;

namespace Blockcore.P2P.Protocol.Payloads
{
    /// <summary>
    /// Ask for the block hashes (inv) that happened since BlockLocator.
    /// </summary>
    [Payload("getblocks")]
    public class GetBlocksPayload : Payload
    {
        private uint version;

        public uint Version
        {
            get
            {
                return this.version;
            }

            set
            {
                this.version = value;
            }
        }

        private BlockLocator blockLocators;

        public BlockLocator BlockLocators
        {
            get
            {
                return this.blockLocators;
            }

            set
            {
                this.blockLocators = value;
            }
        }

        private uint256 hashStop = uint256.Zero;

        public uint256 HashStop { get { return this.hashStop; } set { this.hashStop = value; } }

        public GetBlocksPayload()
        {
        }

        public GetBlocksPayload(BlockLocator locator)
        {
            this.BlockLocators = locator;
        }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.version);
            stream.ReadWrite(ref this.blockLocators);
            stream.ReadWrite(ref this.hashStop);
        }
    }
}