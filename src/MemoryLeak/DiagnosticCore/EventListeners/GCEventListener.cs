﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public interface IChannelReader<T> where T : struct
    {
        ValueTask OnReadResultAsync(CancellationToken cancellationToken);
    }

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

    /// <summary>
    /// payload ref: https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events
    /// </summary>
    public class GCEventListener : ProfileEventListenerBase, IChannelReader<GCStatistics>
    {
        private readonly Channel<GCStatistics> _channel;
        private readonly Func<GCStatistics, Task> _onGCDurationEvent;
        long timeGCStart = 0;
        uint reason = 0;
        uint type = 0;

        public GCEventListener(Func<GCStatistics, Task> onGCDurationEvent) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.GC)
        {
            _onGCDurationEvent = onGCDurationEvent;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<GCStatistics>(channelOption);
        }

        public override void DefaultHandler(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName.StartsWith("GCStart_"))
            {
                timeGCStart = eventData.TimeStamp.Ticks;
                reason = uint.Parse(eventData.Payload[2].ToString());
                type = uint.Parse(eventData.Payload[3].ToString());
            }
            else if (eventData.EventName.StartsWith("GCEnd_"))
            {
                long timeGCEnd = eventData.TimeStamp.Ticks;
                var gcIndex = uint.Parse(eventData.Payload[0].ToString());
                var generation = uint.Parse(eventData.Payload[1].ToString());
                var duration = (double)(timeGCEnd - timeGCStart) / 10.0 / 1000.0;
                // write to channel
                _channel.Writer.TryWrite(new GCStatistics
                {
                    Index = gcIndex,
                    Type = type,
                    Generation = generation,
                    Reason = reason,
                    DurationMillsec = duration,
                    GCStartTime = timeGCStart,
                    GCEndTime = timeGCEnd,
                });
            }
        }

        public async ValueTask OnReadResultAsync(CancellationToken cancellationToken = default)
        {
            // read from channel
            while (Enabled && await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (Enabled && _channel.Reader.TryRead(out var value))
                {
                    await _onGCDurationEvent?.Invoke(value);
                }
            }
        }
    }
}
