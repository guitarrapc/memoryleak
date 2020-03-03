using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public interface IChannelReader<T> where T: struct
    {
        ValueTask OnReadResultAsync(CancellationToken cancellationToken);
    }

    public struct GCDurationResult
    {
        public long Index { get; set; }
        public double DurationMillsec { get; set; }
    }
    public class GCDurationEventListener : ProfileEventListenerBase, IChannelReader<GCDurationResult>
    {
        public ulong countTotalEvents = 0;
        long timeGCStart = 0;

        private readonly Channel<GCDurationResult> _channel;
        private readonly Func<GCDurationResult, Task> _onGCDurationEvent;

        public GCDurationEventListener(Func<GCDurationResult, Task> onGCDurationEvent) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.GC)
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
            if (eventData.EventName == "EventCounters") return;

            // Write the contents of the event to the console.
            if (eventData.EventName.Contains("GCStart"))
            {
                timeGCStart = eventData.TimeStamp.Ticks;
            }
            else if (eventData.EventName.Contains("GCEnd"))
            {
                long timeGCEnd = eventData.TimeStamp.Ticks;
                long gcIndex = long.Parse(eventData.Payload[0].ToString());
                var duration = (double)(timeGCEnd - timeGCStart) / 10.0 / 1000.0;

                // write to channel
                _channel.Writer.TryWrite(new GCDurationResult
                {
                    Index = gcIndex,
                    DurationMillsec = duration
                });
            }

            countTotalEvents++;
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
