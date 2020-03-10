using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public enum ThreadPoolStatisticType
    {
        ThreadWorker,
        ThreadAdjustment,
        IOThread,
    }
    /// <summary>
    /// Data structure represent WorkerThreadPool statistics
    /// </summary>
    public struct EtwThreadPoolStatistics
    {
        public ThreadPoolStatisticType Type { get; set; }
        public ThreadWorkerStatistics ThreadWorker { get; set; }
        public ThreadAdjustmentStatistics ThreadAdjustment { get; set; }
        public IOThreadStatistics IOThread { get; set; }
    }

    public struct ThreadWorkerStatistics
    {
        public long Time { get; set; }
        /// <summary>
        /// Number of worker threads available to process work, including those that are already processing work.
        /// </summary>
        public uint ActiveWrokerThreads { get; set; }
        /// <summary>
        /// Number of worker threads that are not available to process work, but that are being held in reserve in case more threads are needed later.
        /// </summary>
        public uint RetiredWrokerThreads { get; set; }
    }

    public struct IOThreadStatistics
    {
        public long Time { get; set; }
        public uint Count { get; set; }
        public uint RetiredIOThreads { get; set; }
    }

    public struct ThreadAdjustmentStatistics
    {
        public long Time { get; set; }
        public double AverageThrouput { get; set; }

        //public uint NewWorkerThreads { get; set; }

        /// <summary>
        /// Reason	win:UInt32	Reason for the adjustment.
        /// 0x00 - Warmup.
        /// 0x01 - Initializing.
        /// 0x02 - Random move.
        /// 0x03 - Climbing move.
        /// 0x04 - Change point.
        /// 0x05 - Stabilizing.
        /// 0x06 - Starvation.
        /// 0x07 - Thread timed out.
        /// </summary>
        public uint Reason { get; set; }
    }
}
