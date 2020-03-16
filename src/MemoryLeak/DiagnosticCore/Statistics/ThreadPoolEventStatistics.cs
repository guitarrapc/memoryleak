﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public enum ThreadPoolStatisticType
    {
        ThreadPoolWorkerStartStop,
        ThreadPoolAdjustment,
    }
    /// <summary>
    /// Data structure represent WorkerThreadPool statistics
    /// </summary>
    public struct ThreadPoolEventStatistics : IEquatable<ThreadPoolEventStatistics>
    {
        public ThreadPoolStatisticType Type { get; set; }
        public ThreadPoolWorkerStatistics ThreadPoolWorker { get; set; }
        public ThreadPoolAdjustmentStatistics ThreadPoolAdjustment { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ThreadPoolEventStatistics statistics && Equals(statistics);
        }

        public bool Equals([AllowNull] ThreadPoolEventStatistics other)
        {
            return Type == other.Type &&
                   ThreadPoolWorker.Equals(other.ThreadPoolWorker) &&
                   ThreadPoolAdjustment.Equals(other.ThreadPoolAdjustment);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, ThreadPoolWorker, ThreadPoolAdjustment);
        }

        public static bool operator ==(ThreadPoolEventStatistics left, ThreadPoolEventStatistics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ThreadPoolEventStatistics left, ThreadPoolEventStatistics right)
        {
            return !(left == right);
        }
    }

    public struct ThreadPoolWorkerStatistics : IEquatable<ThreadPoolWorkerStatistics>
    {
        public long Time { get; set; }
        /// <summary>
        /// Number of worker threads available to process work, including those that are already processing work.
        /// </summary>
        public uint ActiveWrokerThreads { get; set; }
        /// <summary>
        /// Number of worker threads that are not available to process work, but that are being held in reserve in case more threads are needed later.
        /// Always 0 on ThreadPoolWorkerThreadStart and ThreadPoolWorkerThreadStop
        /// </summary>
        //public uint RetiredWrokerThreads { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ThreadPoolWorkerStatistics statistics && Equals(statistics);
        }

        public bool Equals([AllowNull] ThreadPoolWorkerStatistics other)
        {
            return Time == other.Time &&
                   ActiveWrokerThreads == other.ActiveWrokerThreads;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Time, ActiveWrokerThreads);
        }

        public static bool operator ==(ThreadPoolWorkerStatistics left, ThreadPoolWorkerStatistics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ThreadPoolWorkerStatistics left, ThreadPoolWorkerStatistics right)
        {
            return !(left == right);
        }
    }

    public struct ThreadPoolAdjustmentStatistics : IEquatable<ThreadPoolAdjustmentStatistics>
    {
        public long Time { get; set; }
        public double AverageThrouput { get; set; }
        public uint NewWorkerThreads { get; set; }
        /// <summary>
        /// 0x00 - Warmup.
        /// 0x01 - Initializing.
        /// 0x02 - Random move.
        /// 0x03 - Climbing move.
        /// 0x04 - Change point.
        /// 0x05 - Stabilizing.
        /// 0x06 - Starvation.
        /// 0x07 - Thread timed out.
        /// </summary>
        /// <remarks>
        /// 0x03 isnt usable data, as it were just thread adjustment with hill climing heulistics.
        /// only tracking 0x06 stavation is enough.
        /// </remarks>
        public uint Reason { get; set; }

        public string GetReasonString()
        {
            return Reason switch
            {
                0 => "warmup",
                1 => "itializing",
                2 => "random_move",
                3 => "climbing_move",
                4 => "change_point",
                5 => "stabilizing",
                6 => "starvation",
                7 => "timedout",
                _ => throw new ArgumentOutOfRangeException("reason not defined."),
            };
        }

        public override bool Equals(object obj)
        {
            return obj is ThreadPoolAdjustmentStatistics statistics && Equals(statistics);
        }

        public bool Equals([AllowNull] ThreadPoolAdjustmentStatistics other)
        {
            return Time == other.Time &&
                   AverageThrouput == other.AverageThrouput &&
                   NewWorkerThreads == other.NewWorkerThreads &&
                   Reason == other.Reason;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Time, AverageThrouput, NewWorkerThreads, Reason);
        }

        public static bool operator ==(ThreadPoolAdjustmentStatistics left, ThreadPoolAdjustmentStatistics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ThreadPoolAdjustmentStatistics left, ThreadPoolAdjustmentStatistics right)
        {
            return !(left == right);
        }
    }
}
