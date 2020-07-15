using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace Blockcore.Features.Storage.Models
{
    public class IdentityDocument
    {
        public IdentityDocument()
        {

        }

        public string Owner { get; set; }

        public string Signature { get; set; }

        public IdentityModel Body { get; set; }
    }
}
