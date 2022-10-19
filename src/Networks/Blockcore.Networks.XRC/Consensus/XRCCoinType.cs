using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blockcore.Networks.XRC.Consensus
{
    public class XRCCoinType
    {
        public enum CoinTypes
        {
            /// <summary>
            /// XRhodium Main Network
            /// </summary>
            XRCMain = 10291,

            /// <summary>
            /// Testnet
            /// </summary>
            XRCTest = 1,

            /// <summary>
            /// RegTest
            /// </summary>
            XRCReg = 1
        }
    }
}
