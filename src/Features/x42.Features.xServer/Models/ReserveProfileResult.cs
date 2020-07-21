using System.ComponentModel.DataAnnotations;
using System;

namespace x42.Features.xServer.Models
{
    public class ReserveProfileResult
    {
        public bool Success { get; set; }

        public string ResultMessage { get; set; }

        public string PriceLockId { get; set; }

        public int Status { get; set; }
    }
}
