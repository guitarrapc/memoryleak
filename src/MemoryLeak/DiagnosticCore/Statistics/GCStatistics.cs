using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    /// <summary>
    /// Data structure represent GC statistics
    /// </summary>
    public struct GCStatistics
    {
        public uint Index { get; set; }
        /// <summary>
        /// 0x0 - Blocking garbage collection occurred outside background garbage collection.
        /// 0x1 - Background garbage collection.
        /// 0x2 - Blocking garbage collection occurred during background garbage collection.
        /// </summary>
        public uint Type { get; set; }
        /// <summary>
        /// Gen0-2
        /// </summary>
        public uint Generation { get; set; }
        /// <summary>
        /// 0x0 - Small object heap allocation.
        /// 0x1 - Induced.
        /// 0x2 - Low memory.
        /// 0x3 - Empty.
        /// 0x4 - Large object heap allocation.
        /// 0x5 - Out of space (for small object heap).
        /// 0x6 - Out of space(for large object heap).
        /// 0x7 - Induced but not forced as blocking.
        /// </summary>
        public uint Reason { get; set; }
        public double DurationMillsec { get; set; }
        public long GCStartTime { get; set; }
        public long GCEndTime { get; set; }
    }
}
