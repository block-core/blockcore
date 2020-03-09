using System;

namespace Stratis.Bitcoin.Controllers.Models
{
    /// <summary>
    /// Class representing a banned peer.
    /// </summary>
    public class BannedPeerModel
    {
        public string EndPoint { get; set; }

        public DateTime? BanUntil { get; set; }

        public string BanReason { get; set; }
    }
}