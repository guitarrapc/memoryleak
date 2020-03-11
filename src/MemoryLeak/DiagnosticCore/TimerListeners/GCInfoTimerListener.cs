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
    public class GCInfoTimerListener : TimerListenerBase, IDisposable, IChannelReader
    {
        static int initializedCount;
        static Timer timer;

        public ChannelReader<GCInfoStatistics> Reader { get; set; }

        private readonly Channel<GCInfoStatistics> _channel;
        private readonly Func<GCInfoStatistics, Task> _onEventEmit;
        private readonly Action<Exception> _onEventError;
        private readonly TimeSpan _dueTime;
        private readonly TimeSpan _intervalPeriod;

        private readonly Func<int, ulong> _getGenerationSizeDelegate;
        private readonly Func<int> _getLastGCPercentTimeInGC;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dueTime">The amount of time delay before timer starts.</param>
        /// <param name="intervalPeriod">The time inteval between the invocation of timer.</param>
        public GCInfoTimerListener(Func<GCInfoStatistics, Task> onEventEmit, Action<Exception> onEventError, TimeSpan dueTime, TimeSpan intervalPeriod)
        {
            _onEventEmit = onEventEmit;
            _onEventError = onEventError;
            _dueTime = dueTime;
            _intervalPeriod = intervalPeriod;
            _channel = Channel.CreateBounded<GCInfoStatistics>(new BoundedChannelOptions(50)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            });

            var methodGetGenerationSize = typeof(GC).GetMethod("GetGenerationSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            _getGenerationSizeDelegate = (Func<int, ulong>)methodGetGenerationSize?.CreateDelegate(typeof(Func<int, ulong>));

            var methodGetLastGCPercentTimeInGC = typeof(GC).GetMethod("GetLastGCPercentTimeInGC", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            _getLastGCPercentTimeInGC = (Func<int>)methodGetLastGCPercentTimeInGC?.CreateDelegate(typeof(Func<int>));
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
                var gcHeapSize = GC.GetTotalMemory(false); // bytes
                var gen0Count = GC.CollectionCount(0);
                var gen1Count = GC.CollectionCount(1);
                var gen2Count = GC.CollectionCount(2);
                var memoryInfo = GC.GetGCMemoryInfo();
                var gen0Size = _getGenerationSizeDelegate(0);
                var gen1Size = _getGenerationSizeDelegate(1);
                var gen2Size = _getGenerationSizeDelegate(2);
                var lohSize = _getGenerationSizeDelegate(3);
                var timeInGc = _getLastGCPercentTimeInGC();

                _channel.Writer.TryWrite(new GCInfoStatistics
                {
                    Date = date,
                    Gen0Count　=gen0Count,
                    Gen1Count = gen1Count,
                    Gen2Count = gen2Count,
                    Gen0Size = gen0Size,
                    Gen1Size = gen1Size,
                    Gen2Size = gen2Size,
                    LohSize = lohSize,
                    HeapSize = gcHeapSize,
                    TimeInGc = timeInGc,
                });
            }
            catch (Exception ex)
            {
                _onEventError?.Invoke(ex);
            }
        }
    }
}
