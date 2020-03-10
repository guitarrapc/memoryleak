using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DiagnosticCore.EventListeners
{
    public enum ThreadStatisticType
    {
        ThreadWorker,
        ThreaddAdjustment,
    }
    /// <summary>
    /// Data structure represent WorkerThreadPool statistics
    /// </summary>
    public struct ThreadStatistics
    {
        public ThreadStatisticType Type { get; set; }
        public ThreadWorkerStatistics ThreadWorker { get; set; }
        public ThreadAdjustmentStatistics ThreadAdjustment { get; set; }
    }

    public struct ThreadWorkerStatistics
    {
        public long Time { get; set; }
        public uint ActiveWrokerThreadCount { get; set; }
        public uint RetiredWrokerThreadCount { get; set; }
    }


    public struct ThreadAdjustmentStatistics
    {
        public long Time { get; set; }
        public double AverageThrouput { get; set; }
        public uint NewWorkerThreadCount { get; set; }
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
    public class ThreadEventListener : ProfileEventListenerBase, IChannelReader<ThreadStatistics>
    {
        private readonly Channel<ThreadStatistics> _channel;
        private readonly Func<ThreadStatistics, Task> _onGCDurationEvent;

        public ThreadEventListener(Func<ThreadStatistics, Task> onGCDurationEvent) : base("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, ClrRuntimeEventKeywords.Threading)
        {
            _onGCDurationEvent = onGCDurationEvent;
            var channelOption = new BoundedChannelOptions(50)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            };
            _channel = Channel.CreateBounded<ThreadStatistics>(channelOption);
        }

        public override void EventCreatedHandler(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName == "ThreadPoolWorkerThreadWait") return;
            if (eventData.EventName == "ThreadPoolWorkerThreadAdjustmentAdjustment")
            {
                long time = eventData.TimeStamp.Ticks;
                var averageThroughput = double.Parse(eventData.Payload[0].ToString());
                var newWorkerThreadCount = uint.Parse(eventData.Payload[1].ToString());
                var reason = uint.Parse(eventData.Payload[2].ToString());
                // write to channel
                _channel.Writer.TryWrite(new ThreadStatistics
                {
                    Type = ThreadStatisticType.ThreaddAdjustment,
                    ThreadAdjustment = new ThreadAdjustmentStatistics
                    {
                        Time = time,
                        AverageThrouput = averageThroughput,
                        NewWorkerThreadCount = newWorkerThreadCount,
                        Reason = reason,
                    },
                });
            }
            else if (eventData.EventName.StartsWith("ThreadPoolWorkerThreadStart")) 
            {
                long time = eventData.TimeStamp.Ticks;
                var activeWrokerThreadCount = uint.Parse(eventData.Payload[0].ToString());
                var retiredWrokerThreadCount = uint.Parse(eventData.Payload[1].ToString());
                // write to channel
                _channel.Writer.TryWrite(new ThreadStatistics
                {
                    Type = ThreadStatisticType.ThreadWorker,
                    ThreadWorker = new ThreadWorkerStatistics
                    {
                        Time = time,
                        ActiveWrokerThreadCount = activeWrokerThreadCount,
                        RetiredWrokerThreadCount = retiredWrokerThreadCount,
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
                    await _onGCDurationEvent?.Invoke(value);
                }
            }
        }
    }
}
