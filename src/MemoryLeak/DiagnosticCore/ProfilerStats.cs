using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.Statistics;
using DiagnosticCore.TimerListeners;

namespace DiagnosticCore
{
    public interface IProfilerStat
    {
        void Start();
        void Restart();
        void Stop();
        Task ReadResultAsync(CancellationToken cancellationToken);
    }
    public class GCEventStat : IProfilerStat
    {
        private readonly GCEventListener listener;

        public GCEventStat(Func<EtwGCStatistics, Task> onEventEmi)
        {
            listener = new GCEventListener(onEventEmi);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(GCEventStat)}");
            });
        }

        public void Stop()
        {
            listener.Stop();
        }

        public async Task ReadResultAsync(CancellationToken cancellationToken)
        {
            await listener.OnReadResultAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public class ThreadPoolEventStat : IProfilerStat
    {
        private readonly ThreadPoolEventListener listener;

        public ThreadPoolEventStat(Func<EtwThreadPoolStatistics, Task> onEventEmi)
        {
            listener = new ThreadPoolEventListener(onEventEmi);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(ThreadPoolEventStat)}");
            });
        }

        public void Stop()
        {
            listener.Stop();
        }

        public async Task ReadResultAsync(CancellationToken cancellationToken)
        {
            await listener.OnReadResultAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public class ContentionEventStat : IProfilerStat
    {
        private readonly ContentionEventListener listener;

        public ContentionEventStat(Func<EtwContentionStatistics, Task> onEventEmi)
        {
            listener = new ContentionEventListener(onEventEmi);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(ContentionEventStat)}");
            });
        }

        public void Stop()
        {
            listener.Stop();
        }

        public async Task ReadResultAsync(CancellationToken cancellationToken)
        {
            await listener.OnReadResultAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public class TimerThreadInfoStat : IProfilerStat
    {
        private readonly ThreadInfoLTimerListener listener;

        public TimerThreadInfoStat(Func<TimerThreadInfoStatistics, Task> onEventEmit, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new ThreadInfoLTimerListener(onEventEmit, options.dueTime, options.interval);
        }

        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(() => listener.EventCreatedHandler(), () =>
            {
                Console.WriteLine($"Start: {nameof(TimerThreadInfoStat)}");
            });
        }

        public void Stop()
        {
            listener.Stop();
        }

        public async Task ReadResultAsync(CancellationToken cancellationToken)
        {
            await listener.OnReadResultAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
