using System.ComponentModel.DataAnnotations;
using System;

namespace x42.Features.xServer.Models
{
    public class ProfileReserveRequest
    {
        /// <summary>
        ///     User defined name of profile to be registered.
        /// </summary>
        [Required(ErrorMessage = "A unique name for the profile is missing")]
        [StringLength(64, ErrorMessage = "The profile name cannot exceed 64 characters.")]
        public string Name { get; set; }

        /// <summary>
        ///     The key address used to sign and verify profile ownership.
        /// </summary>
        [Required(ErrorMessage = "The key address is missing.")]
        [StringLength(128, ErrorMessage = "The key address cannot exceed 128 characters.")]
        public string KeyAddress { get; set; }

        /// <summary>
        ///     The hot return address used for payment assigned to this profile.
        /// </summary>
        [Required(ErrorMessage = "The return address is missing.")]
        [StringLength(128, ErrorMessage = "The key address cannot exceed 128 characters.")]
        public string ReturnAddress { get; set; }

        /// <summary>
        ///     The Signature of the profile requesting to be registered.
        /// </summary>
        [Required(ErrorMessage = "The signature is missing.")]
        [StringLength(1024, ErrorMessage = "The signature cannot exceed 1024 characters.")]
        public string Signature { get; set; }
    }
}
