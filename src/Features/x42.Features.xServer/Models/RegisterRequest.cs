using System;
using System.ComponentModel.DataAnnotations;

namespace x42.Features.xServer.Models
{
    public class RegisterRequest
    {
        /// <summary>
        ///     The profile name of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The profile name is missing.")]
        [StringLength(64, ErrorMessage = "The profile name cannot exceed 64 characters.")]
        public string ProfileName { get; set; }

        /// <summary>
        ///     Network Protocol of the server requesting to be registered.
        ///     Support for HTTP = 1 or HTTPS = 2
        /// </summary>
        [Required(ErrorMessage = "The Network protocol is missing.")]
        [Range(1, 100, ErrorMessage = "The network protocol cannot be below 1 and not exceed 100.")]
        public int NetworkProtocol { get; set; }

        /// <summary>
        ///     Network Address of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Network address is missing.")]
        [StringLength(128, ErrorMessage = "The Network Address cannot exceed 128 characters.")]
        public string NetworkAddress { get; set; }

        /// <summary>
        ///     Network Port of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The Port is missing.")]
        [Range(1, 65535, ErrorMessage = "The Port cannot be below 1 and not exceed 65535.")]
        public long NetworkPort { get; set; }

        /// <summary>
        ///     The key address of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The key address is missing.")]
        [StringLength(128, ErrorMessage = "The key address cannot exceed 128 characters.")]
        public string KeyAddress { get; set; }

        /// <summary>
        ///     The sign address of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The sign address is missing.")]
        [StringLength(128, ErrorMessage = "The sign address cannot exceed 128 characters.")]
        public string SignAddress { get; set; }

        /// <summary>
        ///     The fee address of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The fee address is missing.")]
        [StringLength(128, ErrorMessage = "The fee address cannot exceed 128 characters.")]
        public string FeeAddress { get; set; }

        /// <summary>
        ///     The Signature of the server requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The signature is missing.")]
        [StringLength(1024, ErrorMessage = "The signature cannot exceed 1024 characters.")]
        public string Signature { get; set; }

        /// <summary>
        ///     The Tier the server is requesting to register as.
        /// </summary>
        [Required(ErrorMessage = "The Tier is missing.")]
        [Range(1, 65535, ErrorMessage = "The Tier cannot be below 1 and not exceed 65535.")]
        public int Tier { get; set; }
    }
}
