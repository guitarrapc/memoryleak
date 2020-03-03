using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public interface IChannelReader<T> where T : struct
    {
        ValueTask OnReadResultAsync(CancellationToken cancellationToken);
    }

    public struct GCDurationResult
    {
        public uint Index { get; set; }
        public uint Generation { get; set; }
        public uint Reason { get; set; }
        public double DurationMillsec { get; set; }
        public long GCStartTime { get; set; }
        public long GCEndTime { get; set; }
    }

    /// <summary>
    /// payload ref: https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events
    /// </summary>
    public class GCEventListener : ProfileEventListenerBase, IChannelReader<GCDurationResult>
    {
        long timeGCStart = 0;
        uint reason = 0;

        private readonly Channel<GCDurationResult> _channel;
        private readonly Func<GCDurationResult, Task> _onGCDurationEvent;

        public GCEventListener(Func<GCDurationResult, Task> onGCDurationEvent) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.GC)
        {
            _onGCDurationEvent = onGCDurationEvent;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<GCDurationResult>(channelOption);
        }

        public override void DefaultHandler(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName.StartsWith("GCStart_"))
            {
                timeGCStart = eventData.TimeStamp.Ticks;
                reason = uint.Parse(eventData.Payload[2].ToString());
            }
            else if (eventData.EventName.StartsWith("GCEnd_"))
            {
                long timeGCEnd = eventData.TimeStamp.Ticks;
                var gcIndex = uint.Parse(eventData.Payload[0].ToString());
                var generation = uint.Parse(eventData.Payload[1].ToString());
                var duration = (double)(timeGCEnd - timeGCStart) / 10.0 / 1000.0;
                // write to channel
                _channel.Writer.TryWrite(new GCDurationResult
                {
                    Index = gcIndex,
                    Reason = reason,
                    Generation = generation,
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
