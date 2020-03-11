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
        /// <summary>
        /// Start Profiling
        /// </summary>
        void Start();
        /// <summary>
        /// Restart Profiling
        /// </summary>
        void Restart();
        /// <summary>
        /// Stop Profiling
        /// </summary>
        void Stop();
        /// <summary>
        /// Read Profiler statistics
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ReadResultAsync(CancellationToken cancellationToken);
    }
    public class GCEventProfiler : IProfiler
    {
        private readonly GCEventListener listener;

        public GCEventProfiler(Func<GCEventStatistics, Task> onEventEmit, Action<Exception> onEventError)
        {
            listener = new GCEventListener(onEventEmit, onEventError);
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

        public ThreadPoolEventProfiler(Func<ThreadPoolEventStatistics, Task> onEventEmit, Action<Exception> onEventError)
        {
            listener = new ThreadPoolEventListener(onEventEmit, onEventError);
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

        public ContentionEventProfiler(Func<ContentionEventStatistics, Task> onEventEmit, Action<Exception> onEventError)
        {
            listener = new ContentionEventListener(onEventEmit, onEventError);
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

        public ThreadInfoTimerProfiler(Func<ThreadInfoStatistics, Task> onEventEmit, Action<Exception> onEventError, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new ThreadInfoTimerListener(onEventEmit, onEventError, options.dueTime, options.interval);
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

        public GCInfoTimerProfiler(Func<GCInfoStatistics, Task> onEventEmit, Action<Exception> onEventError, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new GCInfoTimerListener(onEventEmit, onEventError, options.dueTime, options.interval);
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

        public ProcessInfoTimerProfiler(Func<ProcessInfoStatistics, Task> onEventEmit, Action<Exception> onEventError, (TimeSpan dueTime, TimeSpan interval) options)
        {
            listener = new ProcessInfoTimerListener(onEventEmit, onEventError, options.dueTime, options.interval);
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
