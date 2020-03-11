using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public enum GCEventType
    {
        GCStartEnd,
        GCSuspend,
        GCHeapStat
    }

    /// <summary>
    /// Data structure represent GC statistics
    /// </summary>
    public struct EtwGCStatistics
    {
        public GCEventType Type { get; set; }
        public GCStartEndStatistics GCStartEndStatistics { get; set; }
        public GCSuspendStatistics GCSuspendStatistics { get; set; }
    }

    public struct GCStartEndStatistics
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

        public string GetReasonString()
        {
            return Reason switch
            {
                0 => "Small object heap allocation",
                1 => "Induced",
                2 => "Low memory",
                3 => "Empty",
                4 => "Large object heap allocation",
                5 => "Out of space (for small object heap)",
                6 => "Out of space(for large object heap)",
                7 => "Induced but not forced as blocking",
                _ => throw new ArgumentOutOfRangeException("reason not defined."),
            };
        }
    }

    public struct GCSuspendStatistics
    {
        public double DurationMillisec { get; set; }
        /// <summary>
        /// 0x0 - Other.
        /// 0x1 - Garbage collection.
        /// 0x2 - Application domain shutdown.
        /// 0x3 - Code pitching.
        /// 0x4 - Shutdown.
        /// 0x5 - Debugger.
        /// 0x6 - Preparation for garbage collection.
        /// </summary>
        public uint Reason { get; set; }
        public uint Count { get; set; }

        public string GetReasonString()
        {
            return Reason switch
            {
                0 => "Other",
                1 => "Garbage collection",
                2 => "Application domain shutdown",
                3 => "Code pitching",
                4 => "Shutdown",
                5 => "Debugger",
                6 => "Preparation for garbage collection",
                _ => throw new ArgumentOutOfRangeException("reason not defined."),
            };
        }
    }
}
