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
    /// EventListener to collect Garbage Collection events. <see cref="EtwGCStatistics"/>.
    /// https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events
    /// </summary>
    public class GCEventListener : ProfileEventListenerBase, IChannelReader
    {
        private readonly Channel<EtwGCStatistics> _channel;
        private readonly Func<EtwGCStatistics, Task> _onEventEmit;
        long timeGCStart = 0;
        uint reason = 0;
        uint type = 0;

        // suspend
        long suspendTimeGCStart = 0;
        uint suspendReason = 0;
        uint suspendCount = 0;

        public GCEventListener(Func<EtwGCStatistics, Task> onEventEmit) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.GC)
        {
            _onEventEmit = onEventEmit;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<EtwGCStatistics>(channelOption);
        }

        /// GC Flow
        ///Foreground (Blocking) GC flow (all Gen 0/1 GCs and full blocking GCs)
        ///* GCSuspendEE_V1
        ///* GCSuspendEEEnd_V1 <– suspension is done
        ///* GCStart_V1
        ///* GCEnd_V1 <– actual GC is done
        ///* GCRestartEEBegin_V1
        ///* GCRestartEEEnd_V1 <– resumption is done.
        ///
        ///Background GC flow (Gen 2)
        ///* GCSuspendEE_V1
        ///* GCSuspendEEEnd_V1
        ///* GCStart_V1 <– Background GC starts
        ///* GCRestartEEBegin_V1
        ///* GCRestartEEEnd_V1 <– done with the initial suspension
        ///* GCSuspendEE_V1
        ///* GCSuspendEEEnd_V1
        ///* GCRestartEEBegin_V1
        ///* GCRestartEEEnd_V1 <– done with Background GC’s own suspension
        ///* GCSuspendEE_V1
        ///* GCSuspendEEEnd_V1 <– suspension for Foreground GC is done
        ///* GCStart_V1
        ///* GCEnd_V1 <– Foreground GC is done
        ///* GCRestartEEBegin_V1
        ///* GCRestartEEEnd_V1 <– resumption for Foreground GC is done
        ///* GCEnd_V1 <– Background GC ends
        ///<remarks>
        /// ref: https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals?redirectedfrom=MSDN#background_garbage_collection
        /// ref: https://mattwarren.org/2016/06/20/Visualising-the-dotNET-Garbage-Collector/
        ///</remarks>
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
                _channel.Writer.TryWrite(new EtwGCStatistics
                {
                    Type = GCEventType.GCStartEnd,
                    GCStartEndStatistics = new GCStartEndStatistics
                    {
                        Index = gcIndex,
                        Type = type,
                        Generation = generation,
                        Reason = reason,
                        DurationMillsec = duration,
                        GCStartTime = timeGCStart,
                        GCEndTime = timeGCEnd,
                    }
                });
            }
            else if (eventData.EventName.StartsWith("GCSuspendEEBegin", StringComparison.OrdinalIgnoreCase))
            {
                suspendTimeGCStart = eventData.TimeStamp.Ticks;
                suspendReason = uint.Parse(eventData.Payload[0].ToString());
                suspendCount = uint.Parse(eventData.Payload[1].ToString());
            }
            else if (eventData.EventName.StartsWith("GCRestartEEEnd", StringComparison.OrdinalIgnoreCase))
            {
                var suspendEnd = eventData.TimeStamp.Ticks;
                var duration = (double)(suspendEnd - suspendTimeGCStart) / 10.0 / 1000.0;

                // write to channel
                _channel.Writer.TryWrite(new EtwGCStatistics
                {
                    Type = GCEventType.GCSuspend,
                    GCSuspendStatistics = new GCSuspendStatistics
                    {
                        Reason = suspendReason,
                        DurationMillisec = duration,
                        Count = suspendCount,
                    }
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
