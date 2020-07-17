using System.ComponentModel.DataAnnotations;
using System;

namespace x42.Features.xServer.Models
{
    public class CreatePriceLockRequest
    {
        /// <summary>
        ///     The request amount to create the price lock on.
        /// </summary>
        [Required(ErrorMessage = "Request amount is missing")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public decimal RequestAmount { get; set; }

        /// <summary>
        ///     The currency pair ID.
        /// </summary>
        [Required(ErrorMessage = "Request Amount Pair is missing")]
        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public int RequestAmountPair { get; set; }

        /// <summary>
        ///     The destination address of the profile requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Destination Address is missing.")]
        [StringLength(128, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public string DestinationAddress { get; set; }

        /// <summary>
        ///     Time to watch in blocks before price lock expires.
        /// </summary>
        [Required(ErrorMessage = "ExpireBlock is missing")]
        [Range(1, long.MaxValue, ErrorMessage = "Value for {0} cannot be below {1} and not exceed {2}.")]
        public long ExpireBlock { get; set; }
    }
}
