using System;
using System.ComponentModel.DataAnnotations;

namespace x42.Features.xServer.Models
{
    public class RegisterRequest
    {
        /// <summary>
        ///     User defined name of server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "A name for the server is missing")]
        [StringLength(32, ErrorMessage = "The server node cannot exceed 32 characters.")]
        public string Name { get; set; }

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
        ///     The Signature of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Signature is missing.")]
        [StringLength(1024, ErrorMessage = "The Signature cannot exceed 1024 characters.")]
        public string Signature { get; set; }

        /// <summary>
        ///     The Public Address of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Address is missing.")]
        [StringLength(128, ErrorMessage = "The Address cannot exceed 128 characters.")]
        public string Address { get; set; }

        /// <summary>
        ///     The Tier the server is requesting to register as.
        /// </summary>
        [Required(ErrorMessage = "The Tier is missing.")]
        [Range(1, 65535, ErrorMessage = "The Tier cannot be below 1 and not exceed 65535.")]
        public int Tier { get; set; }
    }
}
