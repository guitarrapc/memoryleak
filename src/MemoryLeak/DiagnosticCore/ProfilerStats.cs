using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DiagnosticCore.EventListeners;
using DiagnosticCore.Statistics;

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
        public Action<ChannelReader<GCStatistics>> ProfilerCallback { get; set; }

        public GCEventStat(Func<GCStatistics, Task> onEventEmi)
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
        public Action<ChannelReader<ThreadPoolStatistics>> ProfilerCallback { get; set; }

        public ThreadPoolEventStat(Func<ThreadPoolStatistics, Task> onEventEmi)
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
        public Action<ChannelReader<ContentionStatistics>> ProfilerCallback { get; set; }

        public ContentionEventStat(Func<ContentionStatistics, Task> onEventEmi)
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
        public Task ReadResultAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Restart()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
