using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    // dummy class to intelop netcoreapp2.2 and below.
    // https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/libraries/System.Private.CoreLib/src/System/GCMemoryInfo.cs
    public readonly struct GCMemoryInfo
    {
        /// <summary>
        /// High memory load threshold when the last GC occured
        /// </summary>
        public long HighMemoryLoadThresholdBytes { get; }

        /// <summary>
        /// Memory load when the last GC ocurred
        /// </summary>
        public long MemoryLoadBytes { get; }

        /// <summary>
        /// Total available memory for the GC to use when the last GC ocurred.
        ///
        /// If the environment variable COMPlus_GCHeapHardLimit is set,
        /// or "Server.GC.HeapHardLimit" is in runtimeconfig.json, this will come from that.
        /// If the program is run in a container, this will be an implementation-defined fraction of the container's size.
        /// Else, this is the physical memory on the machine that was available for the GC to use when the last GC occurred.
        /// </summary>
        public long TotalAvailableMemoryBytes { get; }

        /// <summary>
        /// The total heap size when the last GC ocurred
        /// </summary>
        public long HeapSizeBytes { get; }

        /// <summary>
        /// The total fragmentation when the last GC ocurred
        ///
        /// Let's take the example below:
        ///  | OBJ_A |     OBJ_B     | OBJ_C |   OBJ_D   | OBJ_E |
        ///
        /// Let's say OBJ_B, OBJ_C and and OBJ_E are garbage and get collected, but the heap does not get compacted, the resulting heap will look like the following:
        ///  | OBJ_A |           F           |   OBJ_D   |
        ///
        /// The memory between OBJ_A and OBJ_D marked `F` is considered part of the FragmentedBytes, and will be used to allocate new objects. The memory after OBJ_D will not be
        /// considered part of the FragmentedBytes, and will also be used to allocate new objects
        /// </summary>
        public long FragmentedBytes { get; }

        internal GCMemoryInfo(long highMemoryLoadThresholdBytes,
                              long memoryLoadBytes,
                              long totalAvailableMemoryBytes,
                              long heapSizeBytes,
                              long fragmentedBytes)
        {
            HighMemoryLoadThresholdBytes = highMemoryLoadThresholdBytes;
            MemoryLoadBytes = memoryLoadBytes;
            TotalAvailableMemoryBytes = totalAvailableMemoryBytes;
            HeapSizeBytes = heapSizeBytes;
            FragmentedBytes = fragmentedBytes;
        }

        public static GCMemoryInfo operator +(GCMemoryInfo left, GCMemoryInfo right)
        {
            return new GCMemoryInfo(
                left.FragmentedBytes + right.FragmentedBytes,
                left.HeapSizeBytes + right.HeapSizeBytes,
                left.HighMemoryLoadThresholdBytes + right.HighMemoryLoadThresholdBytes,
                left.MemoryLoadBytes + right.MemoryLoadBytes,
                left.TotalAvailableMemoryBytes + right.TotalAvailableMemoryBytes);
        }

        public static GCMemoryInfo operator -(GCMemoryInfo left, GCMemoryInfo right)
        {
            return new GCMemoryInfo(
                Math.Max(0, left.FragmentedBytes - right.FragmentedBytes),
                Math.Max(0, left.HeapSizeBytes - right.HeapSizeBytes),
                Math.Max(0, left.HighMemoryLoadThresholdBytes - right.HighMemoryLoadThresholdBytes),
                Math.Max(0, left.MemoryLoadBytes - right.MemoryLoadBytes),
                Math.Max(0, left.TotalAvailableMemoryBytes - right.TotalAvailableMemoryBytes));
        }
    }
}
