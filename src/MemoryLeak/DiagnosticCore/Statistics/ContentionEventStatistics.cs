using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public struct ContentionEventStatistics : IEquatable<ContentionEventStatistics>
    {
        public long Time { get; set; }
        /// <summary>
        /// 0 : managed.
        /// 1 : native
        /// </summary>
        public byte Flag { get; set; }
        public double DurationNs { get; set; }


        public bool Equals([AllowNull] ContentionEventStatistics other)
        {
            return Time == other.Time 
                && Flag == other.Flag 
                && DurationNs == other.DurationNs;
        }
        public override bool Equals(object obj)
        {
            if (obj is ContentionEventStatistics other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Time, Flag, DurationNs);
        }

        public static bool operator ==(ContentionEventStatistics left, ContentionEventStatistics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentionEventStatistics left, ContentionEventStatistics right)
        {
            return !(left == right);
        }
    }
}
