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
    public class ThreadPoolEventListener : ProfileEventListenerBase, IChannelReader<ThreadPoolStatistics>
    {
        private readonly Channel<ThreadPoolStatistics> _channel;
        private readonly Func<ThreadPoolStatistics, Task> _onEventEmit;

        public ThreadPoolEventListener(Func<ThreadPoolStatistics, Task> onEventEmit) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.Threading)
        {
            _onEventEmit = onEventEmit;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<ThreadPoolStatistics>(channelOption);
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
                _channel.Writer.TryWrite(new ThreadPoolStatistics
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

                // get threadpool statistics
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);
                ThreadPool.GetAvailableThreads(out var worker, out var completion);
                var workerThreads = maxWorkerThreads - worker;
                var completionPortThreads = maxCompletionPortThreads - completion;

                // todo: get threadpool property `ThreadPool.ThreadCount`? https://github.com/dotnet/corefx/pull/37401/files

                // write to channel
                _channel.Writer.TryWrite(new ThreadPoolStatistics
                {
                    Type = ThreadPoolStatisticType.ThreadWorker,
                    ThreadWorker = new ThreadWorkerStatistics
                    {
                        Time = time,
                        ActiveWrokerThreads = activeWrokerThreadCount,
                        RetiredWrokerThreads = retiredWrokerThreadCount,
                        WorkerThreads = workerThreads,
                        CompletionPortThreads = completionPortThreads,
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
                _channel.Writer.TryWrite(new ThreadPoolStatistics
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
