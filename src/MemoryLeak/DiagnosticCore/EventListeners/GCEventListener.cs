﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticCore.Statistics;

namespace DiagnosticCore.EventListeners
{
    /// <summary>
    /// EventListener to collect Garbage Collection events. <see cref="GCStatistics"/>.
    /// https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events
    /// </summary>
    public class GCEventListener : ProfileEventListenerBase, IChannelReader<GCStatistics>
    {
        private readonly Channel<GCStatistics> _channel;
        private readonly Func<GCStatistics, Task> _onEventEmit;
        long timeGCStart = 0;
        uint reason = 0;
        uint type = 0;

        public GCEventListener(Func<GCStatistics, Task> onEventEmit) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.GC)
        {
            _onEventEmit = onEventEmit;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<GCStatistics>(channelOption);
        }

        public override void EventCreatedHandler(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName.StartsWith("GCStart_", StringComparison.OrdinalIgnoreCase)) // GCStart_V1 / V2 ...
            {
                timeGCStart = eventData.TimeStamp.Ticks;
                reason = uint.Parse(eventData.Payload[2].ToString());
                type = uint.Parse(eventData.Payload[3].ToString());
            }
            else if (eventData.EventName.StartsWith("GCEnd_", StringComparison.OrdinalIgnoreCase)) // GCEnd_V1 / V2 ...
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
                    await _onEventEmit?.Invoke(value);
                }
            }
        }
    }
}
