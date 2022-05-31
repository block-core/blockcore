using System;
using System.ComponentModel.DataAnnotations;

namespace x42.Features.xServer.Models
{
    public class TestSshCredentialRequest
    {

        /// <summary>
        ///     The IP address of the server to test SSH Credentials against.
        /// </summary>
        [Required(ErrorMessage = "The IP Address is missing.")]
        public string IpAddress { get; set; }

        /// <summary>
        ///     The ssh user to test SSH Credentials against.
        /// </summary>
        [Required(ErrorMessage = "The SSH User is missing.")]
        public string SshUser { get; set; }

        /// <summary>
        ///     The ssh password to test SSH Credentials against.
        /// </summary>
        [Required(ErrorMessage = "The SSH Password missing.")]
        public string SsHPassword { get; set; }

    }
}
