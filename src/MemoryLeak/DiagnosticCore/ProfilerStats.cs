using System;
using System.Threading;
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

        public GCEventStat(Func<GCEventStatistics, Task> onEventEmi)
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

        public ThreadPoolEventStat(Func<ThreadPoolEventStatistics, Task> onEventEmi)
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

        public ContentionEventStat(Func<ContentionEventStatistics, Task> onEventEmi)
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

    public class ThreadInfoTimerStat : IProfilerStat
    {
        private readonly ThreadInfoTimerListener listener;

        public ThreadInfoTimerStat(Func<ThreadInfoStatistics, Task> onEventEmit, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new ThreadInfoTimerListener(onEventEmit, options.dueTime, options.interval);
        }

        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(() => listener.EventCreatedHandler(), () =>
            {
                Console.WriteLine($"Start: {nameof(ThreadInfoTimerStat)}");
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

    public class GCInfoTimerStat : IProfilerStat
    {
        private readonly GCInfoTimerListener listener;

        public GCInfoTimerStat(Func<GCInfoStatistics, Task> onEventEmit, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new GCInfoTimerListener(onEventEmit, options.dueTime, options.interval);
        }

        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(() => listener.EventCreatedHandler(), () =>
            {
                Console.WriteLine($"Start: {nameof(GCInfoTimerStat)}");
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
