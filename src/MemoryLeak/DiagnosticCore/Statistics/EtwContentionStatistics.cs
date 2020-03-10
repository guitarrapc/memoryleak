using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public struct EtwContentionStatistics
    {
        public long Time { get; set; }
        /// <summary>
        /// 0 : managed.
        /// 1 : native
        /// </summary>
        public byte Flag { get; set; }
        public uint DurationNs { get; set; }
    }
}
