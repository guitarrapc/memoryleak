using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public struct GCInfoStatistics
    {
        public DateTime Date { get; set; }
        public long HeapSize { get; set; }
        public int Gen0Count { get; set; }
        public int Gen1Count { get; set; }
        public int Gen2Count { get; set; }
        public int TimeInGc { get; set; }
        public ulong Gen0Size { get; set; }
        public ulong Gen1Size { get; set; }
        public ulong Gen2Size { get; set; }
        public ulong LohSize { get; set; }
    }
}
