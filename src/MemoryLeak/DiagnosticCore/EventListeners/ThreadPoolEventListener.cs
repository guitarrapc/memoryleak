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
    /// EventListener to collect Thread events. <see cref="ThreadStatistics"/>.
    /// </summary>
    /// <remarks>payload: https://docs.microsoft.com/en-us/dotnet/framework/performance/thread-pool-etw-events </remarks>
    public class ThreadPoolEventListener : ProfileEventListenerBase, IChannelReader
    {
        private readonly Channel<ThreadPoolEventStatistics> _channel;
        private readonly Func<ThreadPoolEventStatistics, Task> _onEventEmit;
        private readonly Action<Exception> _onEventError;

        public ThreadPoolEventListener(Func<ThreadPoolEventStatistics, Task> onEventEmit, Action<Exception> onEventError) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.Threading)
        {
            _onEventEmit = onEventEmit;
            _onEventError = onEventError;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<ThreadPoolEventStatistics>(channelOption);
        }

        public override void EventCreatedHandler(EventWrittenEventArgs eventData)
        {
            // ThreadPoolWorkerThreadAdjustmentAdjustment : ThreadPool starvation on Reason 6
            // IOThreadXxxx_ : Windows only.
            if (eventData.EventName.Equals("ThreadPoolWorkerThreadWait", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                if (eventData.EventName.Equals("ThreadPoolWorkerThreadAdjustmentAdjustment", StringComparison.OrdinalIgnoreCase))
                {
                    // do not track on "climing up" reason.
                    var r = eventData.Payload[2].ToString();
                    if (r == "3") return;

                    long time = eventData.TimeStamp.Ticks;
                    var averageThroughput = double.Parse(eventData.Payload[0].ToString());
                    var newWorkerThreadCount = uint.Parse(eventData.Payload[1].ToString());
                    var reason = uint.Parse(r);

                    // write to channel
                    _channel.Writer.TryWrite(new ThreadPoolEventStatistics
                    {
                        Type = ThreadPoolStatisticType.ThreadPoolAdjustment,
                        ThreadPoolAdjustment = new ThreadPoolAdjustmentStatistics
                        {
                            Time = time,
                            NewWorkerThreads = newWorkerThreadCount,
                            AverageThrouput = averageThroughput,
                            Reason = reason,
                        },
                    });
                }
                else if (eventData.EventName.StartsWith("ThreadPoolWorkerThreadStop", StringComparison.OrdinalIgnoreCase))
                {
                    long time = eventData.TimeStamp.Ticks;
                    var activeWrokerThreadCount = uint.Parse(eventData.Payload[0].ToString());
                    // always 0
                    // var retiredWrokerThreadCount = uint.Parse(eventData.Payload[1].ToString());

                    // write to channel
                    _channel.Writer.TryWrite(new ThreadPoolEventStatistics
                    {
                        Type = ThreadPoolStatisticType.ThreadPoolWorkerStartStop,
                        ThreadPoolWorker = new ThreadPoolWorkerStatistics
                        {
                            Time = time,
                            ActiveWrokerThreads = activeWrokerThreadCount,
                        },
                    });
                }
            }
            catch (Exception ex)
            {
                _onEventError?.Invoke(ex);
            }
        }

        public async ValueTask OnReadResultAsync(CancellationToken cancellationToken = default)
        {
            // read from channel
            while (Enabled && await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (Enabled && _channel.Reader.TryRead(out var value))
                {
                    if (_onEventEmit != null)
                    {
                        await _onEventEmit.Invoke(value);
                    }
                }
            }
        }
    }
}
