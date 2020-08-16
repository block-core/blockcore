using System;
using System.Collections.Generic;

namespace Blockcore.Features.NodeHost.Authentication
{
    public class ApiKey
    {
        public int Id { get; set; }

        public bool Enabled { get; set; }

        public string Owner { get; set; }

        public string Key { get; set; }

        //public DateTime Created { get; }

        //public DateTime ValidFrom { get; set; }

        //public DateTime ValidTo { get; set; } // TODO: Add support for time-activated API keys.

        public IReadOnlyCollection<string> Roles { get; set; }

        public IReadOnlyCollection<string> Paths { get; set; }
    }
}
