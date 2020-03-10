using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public enum ThreadPoolStatisticType
    {
        ThreadWorker,
        ThreadAdjustment,
        IOThread,
    }
    /// <summary>
    /// Data structure represent WorkerThreadPool statistics
    /// </summary>
    public struct ThreadPoolStatistics
    {
        public ThreadPoolStatisticType Type { get; set; }
        public ThreadWorkerStatistics ThreadWorker { get; set; }
        public ThreadAdjustmentStatistics ThreadAdjustment { get; set; }
        public IOThreadStatistics IOThread { get; set; }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(ThreadPoolStatistics left, ThreadPoolStatistics right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ThreadPoolStatistics left, ThreadPoolStatistics right)
        {
            return !(left == right);
        }
    }

    public struct ThreadWorkerStatistics
    {
        public long Time { get; set; }
        /// <summary>
        /// Number of worker threads available to process work, including those that are already processing work.
        /// </summary>
        public uint ActiveWrokerThreads { get; set; }
        /// <summary>
        /// Number of worker threads that are not available to process work, but that are being held in reserve in case more threads are needed later.
        /// </summary>
        public uint RetiredWrokerThreads { get; set; }
        /// <summary>
        /// Using WorkerThreads count
        /// </summary>
        public int WorkerThreads { get; set; }
        /// <summary>
        /// Using Asynchronous IO Thread count
        /// </summary>
        public int CompletionPortThreads { get; set; }
    }

    public struct IOThreadStatistics
    {
        public long Time { get; set; }
        public uint Count { get; set; }
        public uint RetiredIOThreads { get; set; }
    }

    public struct ThreadAdjustmentStatistics
    {
        public long Time { get; set; }
        public double AverageThrouput { get; set; }

        //public uint NewWorkerThreads { get; set; }

        /// <summary>
        /// Reason	win:UInt32	Reason for the adjustment.
        /// 0x00 - Warmup.
        /// 0x01 - Initializing.
        /// 0x02 - Random move.
        /// 0x03 - Climbing move.
        /// 0x04 - Change point.
        /// 0x05 - Stabilizing.
        /// 0x06 - Starvation.
        /// 0x07 - Thread timed out.
        /// </summary>
        public uint Reason { get; set; }
    }

    /// <summary>
    /// EventListener to collect Thread events. <see cref="ThreadStatistics"/>.
    /// </summary>
    /// <remarks>payload: https://docs.microsoft.com/en-us/dotnet/framework/performance/thread-pool-etw-events </remarks>
    public class ThreadPoolEventListener : ProfileEventListenerBase, IChannelReader<ThreadPoolStatistics>
    {
        private readonly Channel<ThreadPoolStatistics> _channel;
        private readonly Func<ThreadPoolStatistics, Task> _onEventEmit;
        private readonly int _maxWorkerThreads;
        private readonly int _maxCompletionPortThreads;

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

            ThreadPool.GetMaxThreads(out _maxWorkerThreads, out _maxCompletionPortThreads);
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
                ThreadPool.GetAvailableThreads(out var worker, out var completion);
                var workerThreads = _maxWorkerThreads - worker;
                var completionPortThreads = _maxCompletionPortThreads - completion;

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
