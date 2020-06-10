using System;
using System.ComponentModel.DataAnnotations;

namespace x42.Features.xServer.Models
{
    /// <summary>
    ///     Test request for xServer Ports to see if they are available.
    /// </summary>
    public class TestRequest
    {
        /// <summary>
        ///     Public Network Protocol of the server requesting to be registered.
        ///     Support for HTTP = 1 or HTTPS = 2
        /// </summary>
        [Required(ErrorMessage = "The Network protocol is missing.")]
        [Range(1, 100, ErrorMessage = "The network protocol cannot be below 1 and not exceed 100.")]
        public int NetworkProtocol { get; set; }

        /// <summary>
        ///     Public Network Address of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Network address is missing.")]
        [StringLength(128, ErrorMessage = "The Network Address cannot exceed 128 characters.")]
        public string NetworkAddress { get; set; }

        /// <summary>
        ///     Public Port of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Port is missing.")]
        [Range(1, 65535, ErrorMessage = "The Port cannot be below 1 and not exceed 65535.")]
        public long NetworkPort { get; set; }

        /// <summary>
        ///     The blockchain height to be validated.
        /// </summary>
        [Required(ErrorMessage = "The block height is missing.")]
        public long BlockHeight { get; set; }
    }
}
