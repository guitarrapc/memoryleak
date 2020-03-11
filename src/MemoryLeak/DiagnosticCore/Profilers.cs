using System;
using System.Threading;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.Statistics;
using DiagnosticCore.TimerListeners;

namespace DiagnosticCore
{
    public interface IProfiler
    {
        void Start();
        void Restart();
        void Stop();
        Task ReadResultAsync(CancellationToken cancellationToken);
    }
    public class GCEventProfiler : IProfiler
    {
        private readonly GCEventListener listener;

        public GCEventProfiler(Func<GCEventStatistics, Task> onEventEmit)
        {
            listener = new GCEventListener(onEventEmit);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(GCEventProfiler)}");
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

    public class ThreadPoolEventProfiler : IProfiler
    {
        private readonly ThreadPoolEventListener listener;

        public ThreadPoolEventProfiler(Func<ThreadPoolEventStatistics, Task> onEventEmit)
        {
            listener = new ThreadPoolEventListener(onEventEmit);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(ThreadPoolEventProfiler)}");
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

    public class ContentionEventProfiler : IProfiler
    {
        private readonly ContentionEventListener listener;

        public ContentionEventProfiler(Func<ContentionEventStatistics, Task> onEventEmit)
        {
            listener = new ContentionEventListener(onEventEmit);
        }
        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(eventData => listener.EventCreatedHandler(eventData), () =>
            {
                Console.WriteLine($"Start: {nameof(ContentionEventProfiler)}");
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

    public class ThreadInfoTimerProfiler : IProfiler
    {
        private readonly ThreadInfoTimerListener listener;

        public ThreadInfoTimerProfiler(Func<ThreadInfoStatistics, Task> onEventEmit, (TimeSpan dueTime, TimeSpan interval) options)
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
                Console.WriteLine($"Start: {nameof(ThreadInfoTimerProfiler)}");
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

    public class GCInfoTimerProfiler : IProfiler
    {
        private readonly GCInfoTimerListener listener;

        public GCInfoTimerProfiler(Func<GCInfoStatistics, Task> onEventEmit, (TimeSpan dueTime, TimeSpan interval) options)
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
                Console.WriteLine($"Start: {nameof(GCInfoTimerProfiler)}");
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

    public class ProcessInfoTimerProfiler : IProfiler
    {
        private readonly ProcessInfoTimerListener listener;

        public ProcessInfoTimerProfiler(Func<ProcessInfoStatistics, Task> onEventEmit, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new ProcessInfoTimerListener(onEventEmit, options.dueTime, options.interval);
        }

        public void Restart()
        {
            listener.Restart();
        }

        public void Start()
        {
            listener.RunWithCallback(() => listener.EventCreatedHandler(), () =>
            {
                Console.WriteLine($"Start: {nameof(ProcessInfoTimerProfiler)}");
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
