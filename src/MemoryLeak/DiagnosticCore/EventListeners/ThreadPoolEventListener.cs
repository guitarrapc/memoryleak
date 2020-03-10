using System;
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
        private readonly Channel<EtwThreadPoolStatistics> _channel;
        private readonly Func<EtwThreadPoolStatistics, Task> _onEventEmit;

        public ThreadPoolEventListener(Func<EtwThreadPoolStatistics, Task> onEventEmit) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.Threading)
        {
            _onEventEmit = onEventEmit;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<EtwThreadPoolStatistics>(channelOption);
        }

        public override void EventCreatedHandler(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName.Equals("ThreadPoolWorkerThreadWait", StringComparison.OrdinalIgnoreCase)) return;
            if (eventData.EventName.Equals("ThreadPoolWorkerThreadAdjustmentAdjustment", StringComparison.OrdinalIgnoreCase))
            {
                long time = eventData.TimeStamp.Ticks;
                var averageThroughput = double.Parse(eventData.Payload[0].ToString());
                // non consistence value returns. ignore it.
                //var newWorkerThreadCount = uint.Parse(eventData.Payload[1].ToString());
                var reason = uint.Parse(eventData.Payload[2].ToString());
                // write to channel
                _channel.Writer.TryWrite(new EtwThreadPoolStatistics
                {
                    Type = ThreadPoolStatisticType.ThreadAdjustment,
                    ThreadAdjustment = new ThreadAdjustmentStatistics
                    {
                        Time = time,
                        AverageThrouput = averageThroughput,
                        Reason = reason,
                    },
                });
            }
            else if (eventData.EventName.StartsWith("ThreadPoolWorkerThreadStart", StringComparison.OrdinalIgnoreCase) 
                || eventData.EventName.StartsWith("ThreadPoolWorkerThreadStop", StringComparison.OrdinalIgnoreCase))
            {
                long time = eventData.TimeStamp.Ticks;
                var activeWrokerThreadCount = uint.Parse(eventData.Payload[0].ToString());
                var retiredWrokerThreadCount = uint.Parse(eventData.Payload[1].ToString());

                // write to channel
                _channel.Writer.TryWrite(new EtwThreadPoolStatistics
                {
                    Type = ThreadPoolStatisticType.ThreadWorker,
                    ThreadWorker = new ThreadWorkerStatistics
                    {
                        Time = time,
                        ActiveWrokerThreads = activeWrokerThreadCount,
                        RetiredWrokerThreads = retiredWrokerThreadCount,
                    },
                });
            }
            else if (eventData.EventName.StartsWith("IOThreadCreate_", StringComparison.OrdinalIgnoreCase) 
                || eventData.EventName.StartsWith("IOThreadTerminate_", StringComparison.OrdinalIgnoreCase) 
                || eventData.EventName.StartsWith("IOThreadRetire)_", StringComparison.OrdinalIgnoreCase))
            {
                long time = eventData.TimeStamp.Ticks;
                var count = uint.Parse(eventData.Payload[0].ToString());
                var retiredCount = uint.Parse(eventData.Payload[1].ToString());
                _channel.Writer.TryWrite(new EtwThreadPoolStatistics
                {
                    Type = ThreadPoolStatisticType.IOThread,
                    IOThread = new IOThreadStatistics
                    {
                        Time = time,
                        Count = count,
                        RetiredIOThreads = retiredCount,
                    },
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
