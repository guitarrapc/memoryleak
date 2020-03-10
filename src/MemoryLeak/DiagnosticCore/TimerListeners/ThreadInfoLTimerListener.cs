using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.Statistics;

namespace DiagnosticCore.TimerListeners
{
    public class ThreadInfoLTimerListener : TimerListenerBase, IDisposable, IChannelReader
    {
        static int initializedCount;
        static Timer timer;

        public ChannelReader<TimerThreadInfoStatistics> Reader { get; set; }

        private readonly Channel<TimerThreadInfoStatistics> _channel;
        private readonly Func<TimerThreadInfoStatistics, Task> _onEventEmit;
        private readonly TimeSpan _dueTime;
        private readonly TimeSpan _intervalPeriod;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dueTime">The amount of time delay before timer starts.</param>
        /// <param name="intervalPeriod">The time inteval between the invocation of timer.</param>
        public ThreadInfoLTimerListener(Func<TimerThreadInfoStatistics, Task> onEventEmit, TimeSpan dueTime, TimeSpan intervalPeriod)
        {
            _onEventEmit = onEventEmit;
            _dueTime = dueTime;
            _intervalPeriod = intervalPeriod;
            _channel = Channel.CreateBounded<TimerThreadInfoStatistics>(new BoundedChannelOptions(50)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            });
        }

        protected override void OnEventWritten()
        {
            // allow only 1 execution
            var count = Interlocked.Increment(ref initializedCount);
            if (count != 1) return;

            timer = new Timer(_ =>
            {
                if (!Enabled) return;
                _eventWritten?.Invoke();
            }, null, _dueTime, _intervalPeriod);
        }

        public void Dispose()
        {
            var count = Interlocked.Decrement(ref initializedCount);
            if (count == 0)
            {
                var target = Interlocked.Exchange(ref timer, null);
                if (target != null)
                {
                    target.Dispose();
                }
            }
        }

        public async ValueTask OnReadResultAsync(CancellationToken cancellationToken)
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

        public override void EventCreatedHandler()
        {
            try
            {
                var date = DateTime.Now;

                // todo: get threadpool property `ThreadPool.ThreadCount`? https://github.com/dotnet/corefx/pull/37401/files
                ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);

                _channel.Writer.TryWrite(new TimerThreadInfoStatistics
                {
                    AvailableWorkerThreads = availableWorkerThreads,
                    AvailableCompletionPortThreads = availableCompletionPortThreads,
                    MaxWorkerThreads = maxWorkerThreads,
                    MaxCompletionPortThreads = maxCompletionPortThreads,
                });
            }
            catch (Exception ex)
            {
                // todo: any exception handling
            }
        }
    }
}
