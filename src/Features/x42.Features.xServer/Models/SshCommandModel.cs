using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Features.xServer.Models
{
    public class SshCommandModel
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public int Timeout { get; set; }

    }
}
