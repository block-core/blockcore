using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Consensus;
using Blockcore.Consensus.BlockInfo;

namespace Blockcore.Networks.XRC.Consensus
{
    public class XRCConsensusFactory : ConsensusFactory
    {
        public XRCConsensusFactory() : base()
        {
            this.Protocol = new ConsensusProtocol();
            this.Protocol.ProtocolVersion = 80000; //XRC PROTOCOL
            this.Protocol.MinProtocolVersion = 80000; //XRC PROTOCOL
        }

        public override BlockHeader CreateBlockHeader()
        {
            return new XRCBlockHeader();
        }

    }
}
