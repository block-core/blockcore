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
        }

        public override BlockHeader CreateBlockHeader()
        {
            return new XRCBlockHeader();
        }
    }
}
