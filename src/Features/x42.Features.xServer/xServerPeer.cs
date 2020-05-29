using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace x42.Features.xServer
{
    public class xServerPeer
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public long Priority { get; set; }

        public long Port { get; set; }

        public string Version { get; set; }

        public long ResponseTime { get; set; }

    }
}