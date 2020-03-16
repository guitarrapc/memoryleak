using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DiagnosticCore.Statistics
{
    public struct GCInfoStatistics : IEquatable<GCInfoStatistics>
    {
        public DateTime Date { get; set; }
        public long HeapSize { get; set; }
        public int Gen0Count { get; set; }
        public int Gen1Count { get; set; }
        public int Gen2Count { get; set; }
        /// <summary>
        /// Percent
        /// </summary>
        public int TimeInGc { get; set; }
        /// <summary>
        /// bytes
        /// </summary>
        public ulong Gen0Size { get; set; }
        /// <summary>
        /// bytes
        /// </summary>
        public ulong Gen1Size { get; set; }
        /// <summary>
        /// bytes
        /// </summary>
        public ulong Gen2Size { get; set; }
        /// <summary>
        /// bytes
        /// </summary>
        public ulong LohSize { get; set; }

        public override bool Equals(object obj)
        {
            return obj is GCInfoStatistics statistics && Equals(statistics);
        }

        public bool Equals([AllowNull] GCInfoStatistics other)
        {
            return Date == other.Date &&
                   HeapSize == other.HeapSize &&
                   Gen0Count == other.Gen0Count &&
                   Gen1Count == other.Gen1Count &&
                   Gen2Count == other.Gen2Count &&
                   TimeInGc == other.TimeInGc &&
                   Gen0Size == other.Gen0Size &&
                   Gen1Size == other.Gen1Size &&
                   Gen2Size == other.Gen2Size &&
                   LohSize == other.LohSize;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Date);
            hash.Add(HeapSize);
            hash.Add(Gen0Count);
            hash.Add(Gen1Count);
            hash.Add(Gen2Count);
            hash.Add(TimeInGc);
            hash.Add(Gen0Size);
            hash.Add(Gen1Size);
            hash.Add(Gen2Size);
            hash.Add(LohSize);
            return hash.ToHashCode();
        }

        public static bool operator ==(GCInfoStatistics left, GCInfoStatistics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GCInfoStatistics left, GCInfoStatistics right)
        {
            return !(left == right);
        }
    }
}
