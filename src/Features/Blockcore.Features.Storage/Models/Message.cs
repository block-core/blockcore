using System;
using System.Collections.Generic;
using System.Text;

namespace Blockcore.Features.Storage.Models
{
    public class Message
    {
        public short Version { get; set; }

        /// <summary>
        /// JWT in form of JWS or JWE.
        /// </summary>
        public string Content { get; set; }
    }
}
