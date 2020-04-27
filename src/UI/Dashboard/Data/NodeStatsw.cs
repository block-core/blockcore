using System;

namespace Dashboard.Data
{
    public class NodeStats
    {
        public int ConnectedPeers { get; set; }

        public int ConsensusTip { get; set; }
    }

    public class NodeStatsw
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }
}